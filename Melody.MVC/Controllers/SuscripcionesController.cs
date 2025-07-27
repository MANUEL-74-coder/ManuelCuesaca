using Melody.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.API.Consumer;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<ActionResult> MisSuscripciones()
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
                return RedirectToAction("MisSuscripciones");
            }
        }

        // POST: Suscripciones/AgregarMiembro
        [HttpPost]
        [Authorize(Roles = "userpremium")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AgregarMiembro(int suscripcionId, string email)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var request = new { Email = email };

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
                    TempData["Success"] = "Miembro agregado exitosamente";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Error al agregar miembro: {errorContent}";
                }

                return RedirectToAction("Miembros", new { id = suscripcionId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar miembro: " + ex.Message;
                return RedirectToAction("Miembros", new { id = suscripcionId });
            }
        }

        // GET: Suscripciones/ConfirmarRemover
        [Authorize(Roles = "userpremium")]
        public ActionResult ConfirmarRemover(int suscripcionId, int usuarioId, string nombre)
        {
            ViewBag.SuscripcionId = suscripcionId;
            ViewBag.UsuarioId = usuarioId;
            ViewBag.NombreMiembro = nombre;

            return View();
        }

        // POST: Suscripciones/RemoverMiembro
        [HttpPost]
        [Authorize(Roles = "userpremium")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoverMiembro(int suscripcionId, int usuarioId)
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
                    TempData["Success"] = "Miembro removido exitosamente";
                }
                else
                {
                    TempData["Error"] = "Error al remover miembro";
                }

                return RedirectToAction("Miembros", new { id = suscripcionId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al remover miembro";
                return RedirectToAction("Miembros", new { id = suscripcionId });
            }
        }

        // GET: Suscripciones/ConfirmarCancelacion/{id}
        public async Task<ActionResult> ConfirmarCancelacion(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var response = await Crud<MiSuscripcionResponseDto>.GetWithAuth("mi-suscripcion", token);

                if (!response.TienesSuscripcion || response.Suscripcion?.Id != id)
                {
                    TempData["Error"] = "Suscripción no encontrada";
                    return RedirectToAction("MisSuscripciones");
                }

                return View(response);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar información de suscripción";
                return RedirectToAction("MisSuscripciones");
            }
        }

        // POST: Suscripciones/Cancelar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Cancelar(int suscripcionId)
        {
            try
            {
                var token = _authService.ObtenerToken();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.PutAsync(
                    $"https://localhost:7108/api/Suscripciones/{suscripcionId}",
                    new StringContent("", System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Suscripción cancelada exitosamente";
                }
                else
                {
                    TempData["Error"] = "Error al cancelar suscripción";
                }

                return RedirectToAction("MisSuscripciones");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cancelar suscripción";
                return RedirectToAction("MisSuscripciones");
            }
        }
    }
}