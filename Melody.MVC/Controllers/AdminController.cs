using Melody.API.Consumer;
using Melody.Modelos.DTOs;
using Melody.Modelos;
using Melody.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Melody.MVC.Controllers
{

    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly AuthService _authService;

        public AdminController(AuthService authService)
        {
            _authService = authService;
        }

        // Agrega estos métodos a tu AdminController existente

        // GET: Admin/Canciones - Lista todas las canciones para admin
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Canciones()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var canciones = await Crud<CancionDto>.GetListWithAuth("", token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TerminoBusqueda = "";
                ViewBag.TotalCanciones = canciones?.Count ?? 0;

                return View(canciones ?? new List<CancionDto>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar canciones";
                return View(new List<CancionDto>());
            }
        }

        // GET: Admin/BuscarCanciones - Búsqueda de canciones para admin
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> BuscarCanciones(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                TempData["Error"] = "Por favor ingresa un término de búsqueda";
                return RedirectToAction("Canciones");
            }

            try
            {
                var canciones = await Crud<CancionDto>.GetWithQuery("buscar", q);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TerminoBusqueda = q;
                ViewBag.TotalCanciones = canciones?.Count ?? 0;
                ViewBag.Title = $"Resultados: '{q}' ({canciones?.Count ?? 0} canciones)";

                return View("Canciones", canciones ?? new List<CancionDto>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al realizar la búsqueda";
                return RedirectToAction("Canciones");
            }
        }
        // AGREGA ESTE MÉTODO GET a tu AdminController

        // GET: Admin/EliminarCancion - Vista de confirmación para eliminar
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EliminarCancion(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var cancion = await Crud<CancionDto>.GetByIdWithAuth<CancionDto>(id, token);

                if (cancion == null)
                {
                    TempData["Error"] = "Canción no encontrada";
                    return RedirectToAction("Canciones");
                }

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(cancion);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar la canción";
                return RedirectToAction("Canciones");
            }
        }

        // POST: Admin/EliminarCancion - Confirmar eliminación (mantén tu método actual pero cambia el nombre del parámetro)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EliminarCancionConfirmar(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<CancionDto>.DeleteWithAuth(id, token);

                TempData[resultado ? "Success" : "Error"] = resultado ?
                    "Canción eliminada exitosamente" :
                    "Error al eliminar la canción";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar la canción: {ex.Message}";
            }

            return RedirectToAction("Canciones");
        }



        // GET: Admin/Suscripciones - Gestión de suscripciones
        public async Task<ActionResult> Suscripciones()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var suscripciones = await Crud<SuscripcionAdminDto>.GetAllWithAuth<SuscripcionAdminDto>(token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(suscripciones);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar suscripciones";
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(new List<SuscripcionAdminDto>());
            }
        }

        // GET: Admin/BuscarSuscripciones
        public async Task<IActionResult> BuscarSuscripciones(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction(nameof(Suscripciones));
            }
            try
            {
                var token = _authService.ObtenerToken();
                var suscripciones = await Crud<SuscripcionAdminDto>.GetWithQueryAuth("buscar", q, token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TerminoBusqueda = q;
                return View("Suscripciones", suscripciones ?? new List<SuscripcionAdminDto>());
            }
            catch
            {
                TempData["Error"] = "Error en la búsqueda";
                return RedirectToAction(nameof(Suscripciones));
            }
        }

        // POST: Admin/CancelarSuscripcion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelarSuscripcion(int suscripcionId)
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

                return RedirectToAction("Suscripciones");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cancelar suscripción";
                return RedirectToAction("Suscripciones");
            }
        }


        // GET: Admin/Usuarios - Lista de usuarios
        public async Task<ActionResult> Usuarios()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var usuarios = await Crud<UsuarioAdminDto>.GetAllWithAuth<UsuarioAdminDto>(token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(usuarios);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar usuarios";
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(new List<UsuarioAdminDto>());
            }
        }

        // GET: Admin/BuscarUsuarios
        public async Task<IActionResult> BuscarUsuarios(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction(nameof(Usuarios));
            }
            try
            {
                var token = _authService.ObtenerToken();
                var usuarios = await Crud<UsuarioAdminDto>.GetWithQueryAuth("buscar", q, token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TerminoBusqueda = q;
                return View("Usuarios", usuarios ?? new List<UsuarioAdminDto>());
            }
            catch
            {
                TempData["Error"] = "Error en la búsqueda";
                return RedirectToAction(nameof(Usuarios));
            }
        }
        // POST: Admin/ActivarEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActivarEmail(int usuarioId)
        {
            try
            {
                var token = _authService.ObtenerToken();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.PutAsync(
                    $"https://localhost:7108/api/Usuarios/{usuarioId}/activar-email",
                    new StringContent("", System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Email activado exitosamente";
                }
                else
                {
                    TempData["Error"] = "Error al activar email";
                }

                return RedirectToAction("DetalleUsuario", new { id = usuarioId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al activar email";
                return RedirectToAction("DetalleUsuario", new { id = usuarioId });
            }
        }
        // POST: Admin/EliminarUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EliminarUsuario(int usuarioId)
        {
            try
            {
                var token = _authService.ObtenerToken();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync(
                    $"https://localhost:7108/api/Usuarios/{usuarioId}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Usuario eliminado exitosamente";
                    return RedirectToAction("Usuarios");
                }
                else
                {
                    TempData["Error"] = "Error al eliminar usuario";
                    return RedirectToAction("DetalleUsuario", new { id = usuarioId });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar usuario";
                return RedirectToAction("DetalleUsuario", new { id = usuarioId });
            }
        }

        // GET: Admin/DetalleUsuario/{id} - Vista detalle completo del usuario
        public async Task<IActionResult> DetalleUsuario(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();

                var usuario = await Crud<UsuarioAdminDto>.GetWithAuth(id.ToString(), token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(usuario);
            }
            catch
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToAction("Usuarios");
            }
        }


        // GET: Pagos/Index - Solo para admin
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Pagos()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var pagos = await Crud<Pago>.GetAllWithAuth<Pago>(token);

                ViewBag.Title = "Administrar Pagos";
                return View(pagos);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar pagos";
                return View(new List<Pago>());
            }
        }
        // GET: Admin/BuscarPagos
        public async Task<IActionResult> BuscarPagos(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction(nameof(Pagos));
            }
            try
            {
                var token = _authService.ObtenerToken();
                var pagos = await Crud<Pago>.GetWithQueryAuth("buscar", q, token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TerminoBusqueda = q;
                return View("Pagos", pagos ?? new List<Pago>());
            }
            catch
            {
                TempData["Error"] = "Error en la búsqueda";
                return RedirectToAction(nameof(Pagos));
            }
        }

        // GET: Admin/Estadisticas - Dashboard estadísticas
        public async Task<ActionResult> Estadisticas()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var stats = await Crud<EstadisticasSuscripcionesDto>.GetWithAuth("estadisticas", token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(stats);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar estadísticas";
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(new EstadisticasSuscripcionesDto());
            }
        }
    }
}
