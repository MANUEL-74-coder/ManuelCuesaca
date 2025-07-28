using Melody.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos;
using Melody.Modelos.DTOs;
using Melody.API.Consumer;
using Microsoft.AspNetCore.Authorization;
using Melody.Modelos.DTO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Melody.MVC.Controllers
{
    [Authorize]
    public class PagosController : Controller
    {
        private readonly AuthService _authService;

        // Constantes para claves de sesión
        private const string PAYPAL_ORDER_ID_KEY = "PayPalOrderId";
        private const string PLAN_ID_KEY = "PlanId";

        public PagosController(AuthService authService)
        {
            _authService = authService;
        }

        // Método helper para limpiar sesión
        private void LimpiarSesion()
        {
            HttpContext.Session.Remove(PAYPAL_ORDER_ID_KEY);
            HttpContext.Session.Remove(PLAN_ID_KEY);
        }

        // Método helper para obtener datos de sesión
        private (string orderId, int planId, bool isValid) ObtenerDatosSesion()
        {
            var orderId = HttpContext.Session.GetString(PAYPAL_ORDER_ID_KEY);
            var planId = HttpContext.Session.GetInt32(PLAN_ID_KEY) ?? 0;

            return (orderId ?? string.Empty, planId, !string.IsNullOrEmpty(orderId) && planId > 0);
        }

        // POST: Pagos/CrearOrden
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearOrden(int planId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var request = new { PlanId = planId };
                var response = await Crud<CrearOrdenResponseDto>.PostWithAuth("crear-orden", request, token);

                if (response != null && !string.IsNullOrEmpty(response.ApprovalUrl))
                {
                    // Guardar información en sesión
                    HttpContext.Session.SetString(PAYPAL_ORDER_ID_KEY, response.OrderId);
                    HttpContext.Session.SetInt32(PLAN_ID_KEY, planId);

                    return Redirect(response.ApprovalUrl);
                }

                TempData["Error"] = "No se recibió URL de PayPal";
                return RedirectToAction("Index", "Planes");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index", "Planes");
            }
        }

        // GET: Pagos/PaymentSuccess - CALLBACK DE PAYPAL
        public async Task<ActionResult> PaymentSuccess(string token, string PayerID)
        {
            try
            {
                var authToken = _authService.ObtenerToken();

                // Recuperar información de la sesión
                var (orderId, planId, isValid) = ObtenerDatosSesion();
                if (!isValid)
                {
                    TempData["Error"] = "Error: Información de pago inválida - Sesión perdida";
                    return RedirectToAction("Index", "Planes");
                }

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(PayerID))
                {
                    TempData["Error"] = "Error: Información de PayPal inválida";
                    LimpiarSesion();
                    return RedirectToAction("Index", "Planes");
                }

                // Capturar el pago usando DTO específico
                var captureRequest = new { OrderId = orderId, PlanId = planId };
                var captureResponse = await Crud<CapturarPagoResponseDto>.PostWithAuth("capturar", captureRequest, authToken);

                if (captureResponse != null)
                {
                    LimpiarSesion();

                    // FORZAR LOGOUT para renovar completamente la autenticación
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    // Limpiar Session
                    _authService.Logout();

                    TempData["Success"] = "¡Pago procesado exitosamente! Por favor inicia sesión nuevamente para acceder a todas las funciones Premium";
                    return RedirectToAction("Login", "Auth");
                }

                TempData["Error"] = "Error al procesar el pago en la API";
                LimpiarSesion();
                return RedirectToAction("Index", "Planes");
            }
            catch (Exception ex)
            {
                LimpiarSesion();
                TempData["Error"] = $"Error al procesar el pago: {ex.Message}";
                return RedirectToAction("Index", "Planes");
            }
        }
        // GET: Pagos/PaymentCancel - CALLBACK DE CANCELACIÓN
        public ActionResult PaymentCancel()
        {
            // Limpiar sesión cuando usuario cancela
            LimpiarSesion();

            TempData["Warning"] = "Pago cancelado por el usuario";
            return RedirectToAction("Index", "Planes");
        }

        // GET: Pagos/Confirmacion
        public async Task<ActionResult> Confirmacion(int pagoId = 0)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var suscripcion = await Crud<Suscripcion>.GetWithAuth("mi-suscripcion", token);

                ViewBag.PagoId = pagoId;
                return View(suscripcion);
            }
            catch (Exception ex)
            {
                ViewBag.PagoId = pagoId;
                ViewBag.Error = true;
                return View();
            }
        }

        // GET: Pagos/MisPagos
        public async Task<ActionResult> MisPagos()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var pagos = await Crud<Pago>.GetListWithAuth("mis-pagos", token);

                ViewBag.Title = "Historial de Pagos";
                return View(pagos);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar historial";
                return View(new List<Pago>());
            }
        }

        // GET: Pagos/Details
        public async Task<ActionResult> Details(int pagoId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var pago = await Crud<Pago>.GetByIdWithAuth<Pago>(pagoId, token);

                if (pago == null)
                {
                    TempData["Error"] = "Pago no encontrado";
                    return RedirectToAction("MisPagos");
                }

                ViewBag.Title = "Detalles del Pago";
                return View(pago);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar detalles del pago";
                return RedirectToAction("MisPagos");
            }
        }

        // GET: Pagos/DescargarPDF
        public async Task<ActionResult> DescargarPDF(int pagoId)
        {
            try
            {
                var token = _authService.ObtenerToken();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Usar el endpoint configurado en Program.cs
                var response = await client.GetAsync($"{Crud<Pago>.Endpoint}/{pagoId}/pdf");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Error al descargar comprobante";
                    return RedirectToAction("MisPagos");
                }

                var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                return File(pdfBytes, "application/pdf", $"comprobante-{pagoId}.pdf");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al descargar comprobante";
                return RedirectToAction("MisPagos");
            }
        }
    }
}