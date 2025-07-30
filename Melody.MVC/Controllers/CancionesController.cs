using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;
using Melody.API.Consumer;
using Melody.Modelos;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.Net;

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
        // GET: Detalles de canción
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();

                var cancion = await Crud<CancionDto>.GetByIdWithAuth<CancionDto>(id, token);
                if (cancion == null) return NotFound("Canción no encontrada");

                var artista = await Crud<ArtistaDto>.GetByIdWithAuth<ArtistaDto>(cancion.ArtistaId, token);
                if (artista == null) return NotFound("Artista no encontrado");

                OrdenarCancionesDelArtista(artista, id);
                await CargarPlaylistsUsuario();

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
        [HttpGet]
        [Authorize(Roles = "userpremium")]
        public async Task<IActionResult> Descargar(int id)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authService.ObtenerToken());

                var response = await httpClient.GetAsync($"https://localhost:7108/api/Canciones/descargar/{id}");

                if (!response.IsSuccessStatusCode)
                    return NotFound("Error al descargar la canción");

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = response.Content.Headers.ContentDisposition?.FileName ?? "cancion.mp3";

                // Determinar el Content-Type basado en la extensión
                var contentType = Path.GetExtension(fileName).ToLower() switch
                {
                    ".mp3" => "audio/mpeg",
                    ".wav" => "audio/wav",
                    ".flac" => "audio/flac",
                    _ => "audio/mpeg"
                };

                return File(bytes, contentType, fileName);
            }
            catch
            {
                return BadRequest("Error al descargar");
            }
        }

        // POST: Toggle Me Gusta - AJAX 
        [HttpPost]
        [Authorize(Roles = "userfree,userpremium")]
        public async Task<IActionResult> ToggleMeGustaAjax(int cancionId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<MeGusta>.PostWithAuth($"toggle/{cancionId}", null, token);
                return Json(new
                {
                    success = resultado != null,
                    message = resultado != null ? "Favorito actualizado" : "Error al actualizar favorito"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al procesar favorito" });
            }
        }

        // POST: Agregar canción a playlist 
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "userpremium")]
        public async Task<IActionResult> AgregarAPlaylist(int cancionId, int playlistId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<PlaylistCancion>.PostWithAuth($"?playlistId={playlistId}&cancionId={cancionId}", null, token);

                TempData[resultado != null ? "Success" : "Error"] = resultado != null ?
                    "Canción agregada a la playlist exitosamente" :
                    "Error al agregar la canción a la playlist";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar la canción a la playlist";
            }

            return RedirectToAction("Details", new { id = cancionId });
        }

        // GET: Crear nueva canción
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Create()
        {
            await CargarDatosFormulario();
            return View(new CancionCrearDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Create(CancionCrearDto model)
        {
            if (!ModelState.IsValid)
            {
                await CargarDatosFormulario();
                return View(model);
            }

            try
            {
                var formData = CrearFormDataCancion(model);
                var resultado = await Crud<CancionDto>.PostWithFormData(formData, _authService.ObtenerToken(), "subir");

                if (resultado)
                {
                    TempData["Success"] = "Canción subida exitosamente";
                    return RedirectToAction(nameof(MisCanciones));
                }

                TempData["Error"] = "Error al procesar la solicitud en el servidor";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error inesperado: {ex.Message}";
            }

            await CargarDatosFormulario();
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
                //Configurar token automático
                var token = _authService.ObtenerToken();
                var cancion = await Crud<CancionDto>.GetByIdWithAuth<CancionDto>(id, token);
                if (cancion == null)
                {
                    TempData["Error"] = "Canción no encontrada";
                    return RedirectToAction(nameof(MisCanciones));
                }

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View("Delete", cancion);
            }
            catch
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
                TempData[resultado ? "Success" : "Error"] = resultado ?
                                    "Canción eliminada exitosamente" :
                                    "No se pudo eliminar la canción";
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
                // Configurar token automático
                var token = _authService.ObtenerToken();
                var cancion = await Crud<CancionDto>.GetByIdWithAuth<CancionDto>(id,token);
                if (cancion == null)
                {
                    TempData["Error"] = "Canción no encontrada";
                    return RedirectToAction(nameof(MisCanciones));
                }

                await CargarDatosFormulario();

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
                await CargarDatosFormulario();
                return View(model);
            }

            try
            {
                var formData = CrearFormDataActualizacion(model);
                var resultado = await Crud<CancionDto>.UpdateWithAuth(id, formData, _authService.ObtenerToken());

                TempData[resultado ? "Success" : "Error"] = resultado ?
                    "Canción actualizada exitosamente" :
                    "Error al actualizar la canción";

                if (resultado) return RedirectToAction(nameof(MisCanciones));
            }
            catch
            {
                TempData["Error"] = "Error al actualizar la canción";
            }

            await CargarDatosFormulario();
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
        private void OrdenarCancionesDelArtista(ArtistaDto artista, int cancionActualId)
        {
            if (artista.Canciones?.Any() == true)
            {
                var cancionActual = artista.Canciones.FirstOrDefault(c => c.Id == cancionActualId);
                var otrasCanciones = artista.Canciones.Where(c => c.Id != cancionActualId).ToList();

                var cancionesOrdenadas = new List<CancionDto>();
                if (cancionActual != null) cancionesOrdenadas.Add(cancionActual);
                cancionesOrdenadas.AddRange(otrasCanciones);

                artista.Canciones = cancionesOrdenadas;
            }
        }
        private async Task CargarPlaylistsUsuario()
        {
            if (User.IsInRole("userpremium"))
            {
                try
                {
                    var playlists = await Crud<PlaylistDto>.GetListWithAuth("mis-playlists", _authService.ObtenerToken());
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
        private async Task CargarDatosFormulario()
        {
            ViewBag.CurrentUser = _authService.GetCurrentUser();
            ViewBag.Generos = GetGeneros();
            ViewBag.Albums = await GetMisAlbums();
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

        private MultipartFormDataContent CrearFormDataCancion(CancionCrearDto model)
        {
            var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(model.Titulo ?? ""), "Titulo");
            formData.Add(new StringContent(model.GeneroId.ToString()), "GeneroId");
            formData.Add(new StringContent(model.FechaLanzamiento.ToString("yyyy-MM-dd")), "FechaLanzamiento");

            if (model.AlbumId.HasValue)
                formData.Add(new StringContent(model.AlbumId.Value.ToString()), "AlbumId");

            // Archivo de audio
            var audioContent = new StreamContent(model.ArchivoAudio.OpenReadStream());
            audioContent.Headers.ContentType = new MediaTypeHeaderValue(model.ArchivoAudio.ContentType);
            formData.Add(audioContent, "ArchivoAudio", model.ArchivoAudio.FileName);

            // Imagen opcional
            if (model.ImagenPortada != null)
            {
                var imagenContent = new StreamContent(model.ImagenPortada.OpenReadStream());
                imagenContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImagenPortada.ContentType);
                formData.Add(imagenContent, "ImagenPortada", model.ImagenPortada.FileName);
            }

            return formData;
        }

        private MultipartFormDataContent CrearFormDataActualizacion(ActualizarCancionDto model)
        {
            var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(model.Titulo), "Titulo");
            formData.Add(new StringContent(model.GeneroId.ToString()), "GeneroId");
            formData.Add(new StringContent(model.FechaLanzamiento.ToString("yyyy-MM-dd")), "FechaLanzamiento");

            if (model.AlbumId.HasValue)
                formData.Add(new StringContent(model.AlbumId.Value.ToString()), "AlbumId");

            // Archivos opcionales
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

            return formData;
        }
    }
}