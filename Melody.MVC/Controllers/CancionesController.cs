using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;
using Melody.API.Consumer;
using Melody.Modelos;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace Melody.MVC.Controllers
{
    [Authorize]
    public class CancionesController : Controller
    {
        private readonly AuthService _authService;

        public CancionesController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: Lista pública de canciones
        [AllowAnonymous]
        public IActionResult Index()
        {
            try
            {
                var canciones = Crud<CancionDto>.GetAll();
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(canciones ?? new List<CancionDto>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar canciones";
                return View(new List<CancionDto>());
            }
        }

        [AllowAnonymous] // ← CAMBIO 1: Era [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                CancionDto cancion;
                ArtistaDto artista;

                if (_authService.IsAuthenticated()) // ← CAMBIO 2: Verificar autenticación
                {
                    // Si está logueado, usar método con token
                    var token = _authService.ObtenerToken();
                    cancion = await Crud<CancionDto>.GetByIdWithAuth<CancionDto>(id, token); // ← CAMBIO 3: Con token
                    if (cancion == null)
                    {
                        return NotFound("Canción no encontrada");
                    }

                    if (cancion.ArtistaId <= 0)
                    {
                        return NotFound("Artista no válido");
                    }

                    // Obtener artista CON autenticación 
                    artista = await Crud<ArtistaDto>.GetByIdWithAuth<ArtistaDto>(cancion.ArtistaId, token); // ← CAMBIO 4: Con token

                    // Cargar playlists del usuario si es premium
                    if (User.IsInRole("userpremium"))
                    {
                        try
                        {
                            Crud<PlaylistDto>.Endpoint = "https://localhost:7108/api/Playlists";
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
                    cancion = Crud<CancionDto>.GetById(id);
                    if (cancion == null)
                    {
                        return NotFound("Canción no encontrada");
                    }

                    if (cancion.ArtistaId <= 0)
                    {
                        return NotFound("Artista no válido");
                    }

                    // Obtener artista SIN autenticación
                    artista = Crud<ArtistaDto>.GetById(cancion.ArtistaId);
                    ViewBag.MisPlaylists = new List<PlaylistDto>();
                }

                if (artista == null)
                {
                    return NotFound("Artista no encontrado");
                }

                if (artista.Canciones != null && artista.Canciones.Any())
                {
                    var cancionActual = artista.Canciones.FirstOrDefault(c => c.Id == id);
                    var otrasCanciones = artista.Canciones.Where(c => c.Id != id).ToList();

                    var cancionesOrdenadas = new List<CancionDto>();
                    if (cancionActual != null)
                    {
                        cancionesOrdenadas.Add(cancionActual);
                    }
                    cancionesOrdenadas.AddRange(otrasCanciones);

                    artista.Canciones = cancionesOrdenadas;
                }

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.Artista = artista;
                ViewBag.CancionActualId = id;

                return View(cancion);
            }
            catch (Exception ex)
            {
                return NotFound($"Error: {ex.Message}");
            }
        }

        // POST: Toggle Me Gusta - AJAX (Sin DTOs, sin redirección)
        [HttpPost]
        [Authorize(Roles = "userfree,userpremium")]
        public async Task<IActionResult> ToggleMeGustaAjax(int cancionId)
        {
            try
            {
                var token = _authService.ObtenerToken();

                // Configurar endpoint específico para MeGusta
                Crud<MeGusta>.Endpoint = "https://localhost:7108/api/MeGusta"; // Cambia por tu URL

                // Llamar al endpoint toggle con el ID de la canción
                var resultado = await Crud<MeGusta>.PostWithAuth($"toggle/{cancionId}", null, token);

                if (resultado != null)
                {
                    return Json(new { success = true, message = "Favorito actualizado" });
                }
                else
                {
                    return Json(new { success = false, message = "Error al actualizar favorito" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al procesar favorito" });
            }
        }

        // POST: Agregar canción a playlist - SIN DTOs
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "userpremium")]
        public async Task<IActionResult> AgregarAPlaylist(int cancionId, int playlistId)
        {
            try
            {
                var token = _authService.ObtenerToken();

                // Configurar endpoint específico para PlaylistsCanciones
                Crud<PlaylistCancion>.Endpoint = "https://localhost:7108/api/PlaylistsCanciones"; // Cambia por tu URL

                // Llamar al endpoint con query parameters
                var resultado = await Crud<PlaylistCancion>.PostWithAuth($"?playlistId={playlistId}&cancionId={cancionId}", null, token);

                if (resultado != null)
                {
                    TempData["Success"] = "Canción agregada a la playlist exitosamente";
                }
                else
                {
                    TempData["Error"] = "Error al agregar la canción a la playlist";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar la canción a la playlist";
            }

            return RedirectToAction("Details", new { id = cancionId });
        }

        // GET: Formulario para crear canción
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Create()
        {
            ViewBag.CurrentUser = _authService.GetCurrentUser();
            ViewBag.Generos = GetGeneros();
            ViewBag.Albums = await GetMisAlbums();
            return View(new CancionCrearDto());
        }

        private List<SelectListItem> GetGeneros()
        {
            var generos = Crud<Genero>.GetAll();
            return generos.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Nombre
            }).ToList();
        }

        private async Task<List<SelectListItem>> GetMisAlbums()
        {
            var token = _authService.ObtenerToken();
            var albums = await Crud<Album>.GetListWithAuth("mis-albums", token);
            return albums.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Titulo
            }).ToList();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Create(CancionCrearDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Generos = GetGeneros();
                    ViewBag.Albums = await GetMisAlbums();
                    ViewBag.CurrentUser = _authService.GetCurrentUser();
                    return View(model);
                }

                var token = _authService.ObtenerToken();
                using var formData = new MultipartFormDataContent();

                // Agregar campos de texto
                formData.Add(new StringContent(model.Titulo ?? ""), "Titulo");
                formData.Add(new StringContent(model.GeneroId.ToString()), "GeneroId");
                formData.Add(new StringContent(model.FechaLanzamiento.ToString("yyyy-MM-dd")), "FechaLanzamiento");

                if (model.AlbumId.HasValue)
                {
                    formData.Add(new StringContent(model.AlbumId.Value.ToString()), "AlbumId");
                }

                // Agregar archivo de audio
                var audioContent = new StreamContent(model.ArchivoAudio.OpenReadStream());
                audioContent.Headers.ContentType = new MediaTypeHeaderValue(model.ArchivoAudio.ContentType);
                formData.Add(audioContent, "ArchivoAudio", model.ArchivoAudio.FileName);

                // Agregar imagen si existe
                if (model.ImagenPortada != null)
                {
                    var imagenContent = new StreamContent(model.ImagenPortada.OpenReadStream());
                    imagenContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImagenPortada.ContentType);
                    formData.Add(imagenContent, "ImagenPortada", model.ImagenPortada.FileName);
                }

                var resultado = await Crud<CancionDto>.PostWithFormData(formData, token, "subir");

                if (resultado)
                {
                    TempData["Success"] = "Canción subida exitosamente";
                    return RedirectToAction(nameof(MisCanciones));
                }
                else
                {
                    TempData["Error"] = "Error al procesar la solicitud en el servidor";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error inesperado: {ex.Message}";
            }

            // En caso de error, recargar datos
            ViewBag.Generos = GetGeneros();
            ViewBag.Albums = await GetMisAlbums();
            ViewBag.CurrentUser = _authService.GetCurrentUser();
            return View(model);
        }

        // GET: Mis canciones - SOLO ARTISTAS
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> MisCanciones()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var canciones = await Crud<CancionDto>.GetListWithAuth("mis-canciones", token);
                var albums = await Crud<Album>.GetListWithAuth("mis-albums", token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TotalCanciones = canciones?.Count ?? 0;
                ViewBag.MisAlbums = albums ?? new List<Album>();
                return View(canciones ?? new List<CancionDto>());
            }
            catch
            {
                TempData["Error"] = "Error al cargar sus canciones";
                return View(new List<CancionDto>());
            }
        }

        [HttpGet]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> ConfirmarDelete(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var cancion = Crud<CancionDto>.GetById(id);

                if (cancion == null)
                {
                    TempData["Error"] = "Canción no encontrada";
                    return RedirectToAction(nameof(MisCanciones));
                }

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View("Delete", cancion);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar la canción";
                return RedirectToAction(nameof(MisCanciones));
            }
        }

        // POST: Canciones/Delete/5 - Eliminar directamente desde el modal
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<CancionDto>.DeleteWithAuth(id, token);

                if (resultado)
                {
                    TempData["Success"] = "Canción eliminada exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar la canción";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar la canción: {ex.Message}";
            }

            return RedirectToAction(nameof(MisCanciones));
        }

        // GET: Formulario para editar canción
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var cancion = Crud<CancionDto>.GetById(id);
                ViewBag.Generos = GetGeneros();
                ViewBag.Albums = await GetMisAlbums();
                ViewBag.CurrentUser = _authService.GetCurrentUser();

                var model = new ActualizarCancionDto
                {
                    Titulo = cancion.Titulo,
                    GeneroId = cancion.GeneroId,
                    AlbumId = cancion.AlbumId,
                    FechaLanzamiento = cancion.FechaLanzamiento
                };
                return View(model);
            }
            catch
            {
                TempData["Error"] = "Canción no encontrada";
                return RedirectToAction(nameof(MisCanciones));
            }
        }

        // POST: Actualizar canción
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Edit(int id, ActualizarCancionDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.Generos = GetGeneros();
                ViewBag.Albums = await GetMisAlbums();
                return View(model);
            }

            try
            {
                var token = _authService.ObtenerToken();

                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(model.Titulo), "Titulo");
                formData.Add(new StringContent(model.GeneroId.ToString()), "GeneroId");
                formData.Add(new StringContent(model.FechaLanzamiento.ToString("yyyy-MM-dd")), "FechaLanzamiento");
                if (model.AlbumId.HasValue)
                    formData.Add(new StringContent(model.AlbumId.Value.ToString()), "AlbumId");

                // Agregar archivos si existen
                if (model.ArchivoAudio != null)
                {
                    var audioContent = new StreamContent(model.ArchivoAudio.OpenReadStream());
                    audioContent.Headers.ContentType = new MediaTypeHeaderValue(model.ArchivoAudio.ContentType);
                    formData.Add(audioContent, "ArchivoAudio", model.ArchivoAudio.FileName);
                }

                if (model.ImagenPortada != null)
                {
                    var imagenContent = new StreamContent(model.ImagenPortada.OpenReadStream());
                    imagenContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImagenPortada.ContentType);
                    formData.Add(imagenContent, "ImagenPortada", model.ImagenPortada.FileName);
                }

                var resultado = await Crud<CancionDto>.UpdateWithAuth(id, formData, token);

                if (resultado)
                {
                    TempData["Success"] = "Canción actualizada exitosamente";
                    return RedirectToAction(nameof(MisCanciones));
                }
                else
                {
                    TempData["Error"] = "Error al actualizar la canción";
                }
            }
            catch
            {
                ViewBag.Generos = GetGeneros();
                ViewBag.Albums = await GetMisAlbums();
                TempData["Error"] = "Error al actualizar la canción";
            }

            ViewBag.CurrentUser = _authService.GetCurrentUser();
            return View(model);
        }

        // POST: Agregar canción a álbum
        [HttpPost]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> AgregarAAlbum(int cancionId, int albumId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<CancionDto>.PutWithAuth($"agregar-album/{cancionId}", albumId, token);

                TempData[resultado ? "Success" : "Error"] = resultado ?
                    "Canción agregada al álbum exitosamente" :
                    "Error al agregar la canción al álbum";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la solicitud";
            }

            return RedirectToAction(nameof(MisCanciones));
        }

        // POST: Quitar canción de álbum
        [HttpPost]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> QuitarDeAlbum(int cancionId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<CancionDto>.PutWithAuth($"quitar-album/{cancionId}", null, token);

                TempData[resultado ? "Success" : "Error"] = resultado ?
                    "Canción quitada del álbum exitosamente" :
                    "Error al quitar la canción del álbum";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la solicitud";
            }

            return RedirectToAction(nameof(MisCanciones));
        }

        // GET: Buscar canciones
        [AllowAnonymous]
        public async Task<IActionResult> Buscar(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var canciones = await Crud<CancionDto>.GetWithQuery("buscar", q);
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TerminoBusqueda = q;
                return View(canciones ?? new List<CancionDto>());
            }
            catch
            {
                TempData["Error"] = "Error en la búsqueda";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}