using Melody.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.API.Consumer;
using Microsoft.AspNetCore.Authorization;
using Melody.Modelos;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Melody.MVC.Controllers
{
    [Authorize]
    public class SuscripcionesController : Controller
    {
        private readonly AuthService _authService;

        public SuscripcionesController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: Suscripciones/MisSuscripciones - Dashboard principal
        [Authorize(Roles ="userpremium")]
        public async Task<ActionResult> MiSuscripcion()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var response = await Crud<MiSuscripcionResponseDto>.GetWithAuth("mi-suscripcion", token);
                return View(response);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar suscripción";
                return View(new MiSuscripcionResponseDto { TienesSuscripcion = false });
            }
        }

        // GET: Suscripciones/MiHistorial - Historial de suscripciones
        [Authorize(Roles ="userfree,userpremium")]
        public async Task<ActionResult> MiHistorial()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var historial = await Crud<Suscripcion>.GetListWithAuth("mi-historial", token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.Title = "Mi Historial de Suscripciones";
                return View(historial);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar historial";
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(new List<Suscripcion>());
            }
        }


        // GET: Suscripciones/Miembros/{id} - Gestión familiar
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult> Miembros(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var response = await Crud<MiembrosFamiliaResponseDto>.GetWithAuth($"{id}/miembros", token);
                ViewBag.SuscripcionId = id;
                return View(response);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar miembros familiares";
                return RedirectToAction("MiSuscripcion");
            }
        }

       // POST: Suscripciones/AgregarMiembro - CON MEJOR MANEJO DE ERRORES
        [HttpPost]
        [Authorize(Roles = "userpremium")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AgregarMiembro(int suscripcionId, string email)
        {
            try
            {
                // Validación básica
                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] = "El email es requerido";
                    return RedirectToAction("Miembros", new { id = suscripcionId });
                }

                // Validar formato de email
                if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    TempData["Error"] = "El formato del email no es válido";
                    return RedirectToAction("Miembros", new { id = suscripcionId });
                }

                var token = _authService.ObtenerToken();
                var request = new { Email = email.Trim().ToLower() };

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"https://localhost:7108/api/Suscripciones/{suscripcionId}/agregar-miembro",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"Usuario {email} agregado exitosamente";
                }
                else
                {
                    // Leer el mensaje de error específico del API
                    var errorContent = await response.Content.ReadAsStringAsync();
            
                    // Mapear errores comunes a mensajes más amigables para el usuario
                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.NotFound => "No encontramos una cuenta registrada con ese email.",
                        System.Net.HttpStatusCode.BadRequest when errorContent.Contains("admin") || errorContent.Contains("premium") || errorContent.Contains("artista") => 
                            "Este usuario no puede ser agregado a tu suscripción.",
                        System.Net.HttpStatusCode.BadRequest when errorContent.Contains("límite") => 
                            "Has alcanzado el límite máximo de usuarios en tu plan.",
                        System.Net.HttpStatusCode.BadRequest when errorContent.Contains("acceso premium") => 
                            "Este usuario ya tiene una suscripción activa.",
                        System.Net.HttpStatusCode.BadRequest when errorContent.Contains("agregarte") => 
                            "No puedes agregarte a ti mismo.",
                        _ => "No se pudo agregar el usuario. Verifica que tenga una cuenta válida y disponible."
                    };

                    TempData["Error"] = errorMessage;
                }

                return RedirectToAction("Miembros", new { id = suscripcionId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error de conexión. Inténtalo nuevamente.";
                return RedirectToAction("Miembros", new { id = suscripcionId });
            }
        }
        // AGREGA ESTOS MÉTODOS a tu SuscripcionesController

        // GET: Suscripciones/RemoverUsuario - Vista de confirmación para remover
        [HttpGet]
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult> RemoverUsuario(int suscripcionId, int usuarioId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var response = await Crud<MiembrosFamiliaResponseDto>.GetWithAuth($"{suscripcionId}/miembros", token);

                if (response == null || !response.Miembros.Any())
                {
                    TempData["Error"] = "No se encontraron usuarios adicionales";
                    return RedirectToAction("Miembros", new { id = suscripcionId });
                }

                var usuario = response.Miembros.FirstOrDefault(m => m.UsuarioId == usuarioId);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("Miembros", new { id = suscripcionId });
                }

                ViewBag.SuscripcionId = suscripcionId;
                ViewBag.UsuarioId = usuarioId;
                ViewBag.CurrentUser = _authService.GetCurrentUser();

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar información del usuario";
                return RedirectToAction("Miembros", new { id = suscripcionId });
            }
        }

        // POST: Suscripciones/RemoverUsuarioConfirmar - Confirmar remoción
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult> RemoverUsuarioConfirmar(int suscripcionId, int usuarioId)
        {
            try
            {
                var token = _authService.ObtenerToken();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync(
                    $"https://localhost:7108/api/Suscripciones/{suscripcionId}/miembros/{usuarioId}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Usuario removido exitosamente";
                }
                else
                {
                    TempData["Error"] = "Error al remover usuario";
                }

                return RedirectToAction("Miembros", new { id = suscripcionId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al remover usuario";
                return RedirectToAction("Miembros", new { id = suscripcionId });
            }
        }

        // GET: Suscripciones/CancelarSuscripcion 
        [HttpGet]
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult> CancelarSuscripcion(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var response = await Crud<MiSuscripcionResponseDto>.GetWithAuth("mi-suscripcion", token);

                if (!response.TienesSuscripcion || response.Suscripcion.Id != id)
                {
                    TempData["Error"] = "Suscripción no encontrada o no tienes permisos";
                    return RedirectToAction("MiSuscripcion");
                }

                if (!response.EsPropietario)
                {
                    TempData["Error"] = "Solo el propietario puede cancelar la suscripción";
                    return RedirectToAction("MiSuscripcion");
                }

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(response);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar información de la suscripción";
                return RedirectToAction("MiSuscripcion");
            }
        }

        // POST: Suscripciones/CancelarSuscripcionConfirmar - Confirmar cancelación
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult> CancelarSuscripcionConfirmar(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.PutAsync(
                    $"https://localhost:7108/api/Suscripciones/{id}",
                    new StringContent("", System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    // FORZAR LOGOUT para renovar completamente la autenticación
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    // Limpiar Session y AuthService
                    _authService.Logout();

                    TempData["Success"] = "Suscripción cancelada exitosamente. Por favor inicia sesión nuevamente para actualizar tu perfil.";

                    // Redirigir al login
                    return RedirectToAction("Login", "Auth");
                }
                else
                {
                    TempData["Error"] = "Error al cancelar suscripción";
                    return RedirectToAction("MiSuscripcion");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cancelar suscripción";
                return RedirectToAction("MiSuscripcion");
            }
        }
    }
}