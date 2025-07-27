using Melody.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos;
using Melody.Modelos.DTOs;
using Melody.API.Consumer;
using Microsoft.AspNetCore.Authorization;
using Melody.Modelos.DTO;

namespace Melody.MVC.Controllers
{
    [Authorize]
    public class PagosController : Controller
    {
        private readonly AuthService _authService;

        public PagosController(AuthService authService)
        {
            _authService = authService;
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
                    HttpContext.Session.SetString("PayPalOrderId", response.OrderId);
                    HttpContext.Session.SetInt32("PlanId", planId);

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
                var orderId = HttpContext.Session.GetString("PayPalOrderId");
                var planId = HttpContext.Session.GetInt32("PlanId") ?? 0;

                if (string.IsNullOrEmpty(orderId) || planId == 0)
                {
                    TempData["Error"] = "Error: Información de pago inválida - Sesión perdida";
                    return RedirectToAction("Index", "Planes");
                }

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(PayerID))
                {
                    TempData["Error"] = "Error: Información de PayPal inválida";
                    return RedirectToAction("Index", "Planes");
                }

                // Capturar el pago usando DTO específico
                var captureRequest = new { OrderId = orderId, PlanId = planId };
                var captureResponse = await Crud<CapturarPagoResponseDto>.PostWithAuth("capturar", captureRequest, authToken);

                if (captureResponse != null)
                {
                    // Limpiar sesión después del éxito
                    HttpContext.Session.Remove("PayPalOrderId");
                    HttpContext.Session.Remove("PlanId");

                    TempData["Success"] = "¡Pago procesado exitosamente! Bienvenido a Melody Premium";
                    return RedirectToAction("Confirmacion", new { pagoId = captureResponse.PagoId });
                }

                TempData["Error"] = "Error al procesar el pago en la API";
                return RedirectToAction("Index", "Planes");
            }
            catch (Exception ex)
            {
                // Limpiar sesión en caso de error
                HttpContext.Session.Remove("PayPalOrderId");
                HttpContext.Session.Remove("PlanId");

                TempData["Error"] = $"Error al procesar el pago: {ex.Message}";
                return RedirectToAction("Index", "Planes");
            }
        }

        // GET: Pagos/PaymentCancel - CALLBACK DE CANCELACIÓN
        public ActionResult PaymentCancel()
        {
            HttpContext.Session.Remove("PayPalOrderId");
            HttpContext.Session.Remove("PlanId");

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

        // GET: Pagos/Index
        public async Task<ActionResult> Index()
        {
            var token = _authService.ObtenerToken();
            var data = await Crud<Pago>.GetAllWithAuth<Pago>(token);
            return View(data);
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

                var response = await client.GetAsync($"https://localhost:7108/api/Pagos/{pagoId}/pdf");

                if (response.IsSuccessStatusCode)
                {
                    var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                    return File(pdfBytes, "application/pdf", $"comprobante-{pagoId}.pdf");
                }

                TempData["Error"] = "Error al descargar comprobante";
                return RedirectToAction("MisPagos");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al descargar comprobante";
                return RedirectToAction("MisPagos");
            }
        }
    }
}