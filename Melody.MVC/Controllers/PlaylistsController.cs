using Humanizer;
using Melody.API.Consumer;
using Melody.Modelos;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Melody.MVC.Controllers
{
    public class PlaylistsController : Controller
    {
        private readonly AuthService _authService;

        public PlaylistsController(AuthService authService)
        {
            _authService = authService;
        }
        // GET: PlaylistsController
        [AllowAnonymous]
        public async Task<ActionResult> Index()
        {
            try
            {
                // Opción 1: Sin autenticación (para endpoints públicos)
                var playlists = Crud<PlaylistDto>.GetAll();
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(playlists ?? new List<PlaylistDto>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar playlists";
                return View(new List<PlaylistDto>());
            }
        }
        // GET: Mis playlist solo para usuarios premium
        [Authorize(Roles = "userpremium,userfree")]
        public async Task<IActionResult> MisPlaylists()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var playlists = await Crud<PlaylistDto>.GetListWithAuth("mis-playlists", token);
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TotalPlaylists = playlists?.Count ?? 0;
                return View(playlists ?? new List<PlaylistDto>());
            }
            catch
            {
                TempData["Error"] = "Error al cargar sus playlists";
                return View(new List<PlaylistDto>());
            }
        }

        // GET: PlaylistsController/Details/5
        [Authorize]
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var playlist = await Crud<PlaylistDto>.GetByIdWithAuth<PlaylistDto>(id, token);
                if (playlist == null)
                {
                    return NotFound();
                }

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(playlist);
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: PlaylistsController/Create
        [Authorize(Roles = "userpremium")]
        public ActionResult Create()
        {
            ViewBag.CurrentUser = _authService.GetCurrentUser();
            return View(new CrearPlaylistDto());
        }

        // POST: PlaylistsController/Create

        // POST: PlaylistsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult> Create(CrearPlaylistDto dto)
        {
            ViewBag.CurrentUser = _authService.GetCurrentUser();
            ViewBag.Title = "Crear Nueva Playlist";

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            try
            {
                var token = _authService.ObtenerToken();
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Login", "Auth");
                }

                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(dto.Nombre), "Nombre");
                formData.Add(new StringContent(dto.EsPublica.ToString().ToLower()), "EsPublica");

                if (dto.Imagen != null && dto.Imagen.Length > 0)
                {
                    var imagenContent = new StreamContent(dto.Imagen.OpenReadStream());
                    imagenContent.Headers.ContentType = new MediaTypeHeaderValue(dto.Imagen.ContentType);
                    formData.Add(imagenContent, "Imagen", dto.Imagen.FileName);
                }

                var resultado = await Crud<PlaylistDto>.PostWithFormData(formData, token);
                if (resultado)
                {
                    TempData["Success"] = "Playlist creada exitosamente";
                    return RedirectToAction(nameof(MisPlaylists));
                }
                else
                {
                    TempData["Error"] = "Error al crear la playlist";
                    return View(dto);
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Error de conexión. Intente nuevamente.";
                return View(dto);
            }
            catch (Exception)
            {
                TempData["Error"] = "Error interno del servidor";
                return View(dto);
            }
        }

        // GET: PlaylistsController/Edit/5
        [Authorize(Roles = "userpremium,userfree")]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var playlist = await Crud<PlaylistDto>.GetByIdWithAuth<PlaylistDto>(id, token);
                if (playlist == null)
                {
                    TempData["Error"] = "Playlist no encontrada";
                    return RedirectToAction(nameof(MisPlaylists));
                }

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.PlaylistId = id;
                ViewBag.ImagenActual = playlist.Imagen;
                ViewBag.Title = $"Editar {playlist.Nombre}";

                var model = new ActualizarPlaylistDto
                {
                    Nombre = playlist.Nombre,
                    EsPublica = playlist.EsPublica
                };

                return View(model);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No tiene permisos para editar esta playlist";
                return RedirectToAction(nameof(MisPlaylists));
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al cargar la playlist";
                return RedirectToAction(nameof(MisPlaylists));
            }
        }

        // POST: PlaylistsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "userpremium,userfree")]
        public async Task<ActionResult> Edit(int id, ActualizarPlaylistDto dto)
        {
            ViewBag.CurrentUser = _authService.GetCurrentUser();
            ViewBag.PlaylistId = id;
            ViewBag.Title = "Editar Playlist";

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            try
            {
                var token = _authService.ObtenerToken();
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Login", "Auth");
                }

                using var formData = new MultipartFormDataContent();

                if (!string.IsNullOrEmpty(dto.Nombre))
                {
                    formData.Add(new StringContent(dto.Nombre), "Nombre");
                }

                if (dto.EsPublica.HasValue)
                {
                    formData.Add(new StringContent(dto.EsPublica.Value.ToString().ToLower()), "EsPublica");
                }

                if (dto.Imagen != null && dto.Imagen.Length > 0)
                {
                    var imagenContent = new StreamContent(dto.Imagen.OpenReadStream());
                    imagenContent.Headers.ContentType = new MediaTypeHeaderValue(dto.Imagen.ContentType);
                    formData.Add(imagenContent, "Imagen", dto.Imagen.FileName);
                }

                var resultado = await Crud<PlaylistDto>.UpdateWithAuth(id, formData, token);
                if (resultado)
                {
                    TempData["Success"] = "Playlist actualizada exitosamente";
                    return RedirectToAction(nameof(MisPlaylists));
                }
                else
                {
                    TempData["Error"] = "Error al actualizar la playlist";
                    return View(dto);
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Error de conexión o permisos insuficientes";
                return View(dto);
            }
            catch (Exception)
            {
                TempData["Error"] = "Error interno del servidor";
                return View(dto);
            }
        }


        // GET: Confirmar eliminación
        [HttpGet]
        [Authorize(Roles = "userpremium,userfree")]
        public async Task<ActionResult> ConfirmarDelete(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var playlist = await Crud<PlaylistDto>.GetByIdWithAuth<PlaylistDto>(id, token);
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View("Delete", playlist);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar la playlist";
                return RedirectToAction(nameof(MisPlaylists));
            }
        }

        // POST: Eliminar playlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "userpremium,userfree")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<PlaylistDto>.DeleteWithAuth(id, token);

                if (resultado)
                {
                    TempData["Success"] = "Playlist eliminada exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar la playlist";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar la playlist: {ex.Message}";
            }

            return RedirectToAction(nameof(MisPlaylists));
        }
        // POST: Eliminar canción de playlist - AGREGAR ESTE MÉTODO
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "userpremium,userfree")]
        public async Task<IActionResult> EliminarCancion(int playlistCancionId, int playlistId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<PlaylistCancion>.DeleteWithAuth(playlistCancionId, token);

                if (resultado)
                {
                    TempData["Success"] = "Canción eliminada de la playlist";
                }
                else
                {
                    TempData["Error"] = "Error al eliminar la canción";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la solicitud";
            }

            return RedirectToAction("Details", new { id = playlistId });
        }

        // GET: PlaylistsController/Buscar
        [AllowAnonymous]
        public async Task<IActionResult> Buscar(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var playlists = await Crud<PlaylistDto>.GetWithQuery("buscar", q);
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TerminoBusqueda = q;
                return View(playlists ?? new List<PlaylistDto>());
            }
            catch
            {
                TempData["Error"] = "Error en la búsqueda";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
