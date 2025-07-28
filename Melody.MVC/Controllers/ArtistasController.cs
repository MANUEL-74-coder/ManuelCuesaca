using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;
using Melody.API.Consumer;
using System.Net.Http.Headers;
using Melody.Modelos;
using Newtonsoft.Json.Serialization;
using System.Runtime.InteropServices;
using NuGet.Common;

namespace Melody.MVC.Controllers
{
    public class ArtistasController : Controller
    {
        private readonly AuthService _authService;

        public ArtistasController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: Lista pública de artistas
        [AllowAnonymous]
        public IActionResult Index()
        {
            try
            {
                var artistas = Crud<Artista>.GetAll();
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(artistas ?? new List<Artista>());
            }
            catch
            {
                TempData["Error"] = "Error al cargar artistas";
                return View(new List<Artista>());
            }
        }
        // GET: Perfil público de artista
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                ArtistaDto artista;
                if (_authService.IsAuthenticated())
                {
                    // Si está logueado, usar método con token
                    var token = _authService.ObtenerToken();
                    artista = await Crud<ArtistaDto>.GetByIdWithAuth<ArtistaDto>(id, token);

                    // AGREGAR ESTO: Obtener playlists si es usuario premium
                    var currentUser = _authService.GetCurrentUser();
                    if (currentUser?.IsUserPremium == true)
                    {
                        try
                        {
                            var playlists = await Crud<PlaylistDto>.GetListWithAuth("mis-playlists", token);
                            ViewBag.MisPlaylists = playlists ?? new List<PlaylistDto>();
                        }
                        catch
                        {
                            ViewBag.MisPlaylists = new List<PlaylistDto>();
                        }
                    }
                    else
                    {
                        ViewBag.MisPlaylists = new List<PlaylistDto>();
                    }
                }
                else
                {
                    // Si no está logueado, usar método sin token
                    artista = Crud<ArtistaDto>.GetById(id);
                    ViewBag.MisPlaylists = new List<PlaylistDto>();
                }

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(artista);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSeguir(int artistaId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var dto = new { ArtistaId = artistaId };
                var resultado = await Crud<object>.PostWithAuth("toggle", dto, token);
            }
            catch
            {
                TempData["Error"] = "Error al procesar la solicitud";
            }

            return RedirectToAction("Details", new { id = artistaId });
        }

        // GET: Mi perfil de artista
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> MiPerfil()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var perfil = await Crud<ArtistaDto>.GetWithAuth("mi-perfil", token);
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(perfil);
            }
            catch
            {
                TempData["Error"] = "Error al cargar el perfil de artista";
                return RedirectToAction("Index", "Home");
            }
        }
        // GET: Editar perfil de artista
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> EditarPerfil()
        {
            ViewBag.CurrentUser = _authService.GetCurrentUser();

            try
            {
                var token = _authService.ObtenerToken();
                var perfilActual = await Crud<Artista>.GetWithAuth("mi-perfil", token);
                var model = new ActualizarPerfilArtistaDto
                {
                    NombreArtista = perfilActual.NombreArtista,
                    Biografia = perfilActual.Biografia
                };
                ViewBag.ImagenPerfilActual = perfilActual.ImagenPerfil;

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el perfil del artista";
                return RedirectToAction("MiPerfil");
            }
        }

        // POST: Actualizar perfil de artista
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> EditarPerfil(ActualizarPerfilArtistaDto data)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(data);
            }

            try
            {
                var token = _authService.ObtenerToken();
                using var formData = new MultipartFormDataContent();

                if (!string.IsNullOrEmpty(data.NombreArtista))
                    formData.Add(new StringContent(data.NombreArtista), "NombreArtista");
                if (!string.IsNullOrEmpty(data.Biografia))
                    formData.Add(new StringContent(data.Biografia), "Biografia");
                if (data.ImagenPerfil != null)
                {
                    var imagenContent = new StreamContent(data.ImagenPerfil.OpenReadStream());
                    imagenContent.Headers.ContentType = new MediaTypeHeaderValue(data.ImagenPerfil.ContentType);
                    formData.Add(imagenContent, "ImagenPerfil", data.ImagenPerfil.FileName);
                }

                var resultado = await Crud<Artista>.UpdateWithFormData("mi-perfil", formData, token);


                if (resultado)
                {
                    TempData["Success"] = "Perfil de artista actualizado exitosamente";
                    return RedirectToAction(nameof(MiPerfil));
                }
                else
                {
                    TempData["Error"] = "Error al actualizar el perfil de artista";
                }
            }
            catch
            {
                TempData["Error"] = "Error al actualizar el perfil";
            }

            ViewBag.CurrentUser = _authService.GetCurrentUser();
            return View(data);
        }

        // GET: Buscar artistas
        [AllowAnonymous]
        public async Task<IActionResult> Buscar(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var artistas = await Crud<Artista>.GetWithQuery("buscar", q);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TerminoBusqueda = q;
                return View(artistas);
            }
            catch
            {
                TempData["Error"] = "Error en la búsqueda";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}