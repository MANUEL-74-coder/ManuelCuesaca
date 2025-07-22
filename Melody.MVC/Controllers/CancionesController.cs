using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;
using Melody.API.Consumer;
using Melody.Modelos;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace Melody.MVC.Controllers
{
    [Authorize] // Base: requiere autenticación
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
        // GET: Detalles de canción (público)
        [Authorize]
        public IActionResult Details(int id)
        {
            try
            {
                var cancion = Crud<CancionDto>.GetById(id);
                if (cancion == null)
                {
                    return NotFound("Canción no encontrada");
                }

                if (cancion.ArtistaId <= 0)
                {
                    return NotFound("Artista no válido");
                }

                var artista = Crud<ArtistaDto>.GetById(cancion.ArtistaId);
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

                // Pasamos la canción como modelo principal
                return View(cancion);
            }
            catch (Exception ex)
            {
                return NotFound($"Error: {ex.Message}");
            }
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

        // En tu CancionesController.cs, ajusta el método Delete así:

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