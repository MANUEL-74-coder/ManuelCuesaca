using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;
using Melody.API.Consumer;
using Melody.Modelos;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Melody.MVC.Controllers
{
    [Authorize] // Base: requiere autenticación
    public class AlbumsController : Controller
    {
        private readonly AuthService _authService;

        public AlbumsController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: Lista pública de álbumes
        [AllowAnonymous]
        public IActionResult Index()
        {
            try
            {
                var albums = Crud<AlbumDto>.GetAll();
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(albums ?? new List<AlbumDto>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar álbumes";
                return View(new List<AlbumDto>());
            }
        }

        // GET: Detalles de álbum (público)
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var album = Crud<AlbumDto>.GetById(id);
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                var currentUser = _authService.GetCurrentUser();
                if (currentUser?.IsArtista == true)
                {
                    var token = _authService.ObtenerToken();
                    try
                    {
                        var artista = await Crud<ArtistaDto>.GetWithAuth("mi-perfil", token);
                        ViewBag.ArtistaActualId = artista?.Id;
                    }
                    catch
                    {
                        ViewBag.ArtistaActualId = null;
                    }
                }
                return View(album);
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: Formulario para crear álbum
        [Authorize(Roles = "artista")]
        public IActionResult Create()
        {
            ViewBag.CurrentUser = _authService.GetCurrentUser();
            ViewBag.Generos = GetGeneros();
            return View(new CrearAlbumDto());
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

        // POST: Crear álbum
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Create(CrearAlbumDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.Generos = GetGeneros();
                return View(model);
            }

            try
            {
                var token = _authService.ObtenerToken();
                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(model.Titulo), "Titulo");
                formData.Add(new StringContent(model.GeneroId.ToString()), "GeneroId");

                // Agregar portada si existe
                if (model.Portada != null)
                {
                    var imagenContent = new StreamContent(model.Portada.OpenReadStream());
                    imagenContent.Headers.ContentType = new MediaTypeHeaderValue(model.Portada.ContentType);
                    formData.Add(imagenContent, "Portada", model.Portada.FileName);
                }

                var resultado = await Crud<AlbumDto>.PostWithFormData(formData, token, "subir-album");
                if (resultado)
                {
                    TempData["Success"] = "Álbum creado exitosamente";
                    return RedirectToAction(nameof(MisAlbums));
                }
                else
                {
                    TempData["Error"] = "Error al crear álbum";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear el álbum: {ex.Message}";
            }

            ViewBag.CurrentUser = _authService.GetCurrentUser();
            ViewBag.Generos = GetGeneros();
            return View(model);
        }

        private async Task<List<CancionDto>> GetCancionesSinAlbum()
        {
            var token = _authService.ObtenerToken();
            var canciones = await Crud<CancionDto>.GetListWithAuth("mis-canciones", token);
            return canciones?.Where(c => string.IsNullOrEmpty(c.AlbumNombre) || c.AlbumNombre == "Sin Álbum").ToList()
                   ?? new List<CancionDto>();
        }


        // GET: Mis álbumes - SOLO ARTISTAS
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> MisAlbums()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var albums = await Crud<Album>.GetListWithAuth("mis-albums", token);
                var cancionesSinAlbum = await GetCancionesSinAlbum();

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TotalAlbums = albums?.Count ?? 0;
                ViewBag.CancionesSinAlbum = cancionesSinAlbum;

                return View(albums ?? new List<Album>());
            }
            catch
            {
                TempData["Error"] = "Error al cargar sus álbumes";
                return View(new List<Album>());
            }
        }
        // POST: Agregar canción a álbum
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> AgregarCancionAAlbum(int albumId, int cancionId)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<Album>.PutWithAuth($"agregar-cancion/{albumId}", cancionId, token);

                if (resultado)
                {
                    TempData["Success"] = "Canción agregada al álbum exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo agregar la canción al álbum";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al agregar la canción: {ex.Message}";
            }
            return RedirectToAction(nameof(MisAlbums));
        }

        // GET: Formulario para editar álbum
        [Authorize(Roles = "artista")]
        public IActionResult Edit(int id)
        {
            try
            {
                var album = Crud<AlbumDto>.GetById(id);
                ViewBag.Generos = GetGeneros();
                ViewBag.CurrentUser = _authService.GetCurrentUser();

                var model = new ActualizarAlbumDto
                {
                    Titulo = album.Titulo,
                    GeneroId = album.GeneroId
                };

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Álbum no encontrado";
                return RedirectToAction(nameof(MisAlbums));
            }
        }

        // POST: Actualizar álbum
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Edit(int id, ActualizarAlbumDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.Generos = GetGeneros();
                return View(model);
            }

            try
            {
                var token = _authService.ObtenerToken();

                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(model.Titulo), "Titulo");
                formData.Add(new StringContent(model.GeneroId.ToString()), "GeneroId");

                // Agregar portada si existe
                if (model.Portada != null)
                {
                    var imagenContent = new StreamContent(model.Portada.OpenReadStream());
                    imagenContent.Headers.ContentType = new MediaTypeHeaderValue(model.Portada.ContentType);
                    formData.Add(imagenContent, "Portada", model.Portada.FileName);
                }

                var resultado = await Crud<AlbumDto>.UpdateWithAuth(id, formData, token);

                if (resultado)
                {
                    TempData["Success"] = "Álbum actualizado exitosamente";
                    return RedirectToAction(nameof(MisAlbums));
                }
                else
                {
                    TempData["Error"] = "Error al actualizar el álbum";
                }
            }
            catch
            {
                TempData["Error"] = "Error al actualizar el álbum";
            }

            ViewBag.CurrentUser = _authService.GetCurrentUser();
            ViewBag.Generos = GetGeneros();
            return View(model);
        }
        // GET: Confirmar eliminación
        [HttpGet]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> ConfirmarDelete(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var album = Crud<AlbumDto>.GetById(id);

                if (album == null)
                {
                    TempData["Error"] = "Álbum no encontrado";
                    return RedirectToAction(nameof(MisAlbums));
                }

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View("Delete", album);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el álbum";
                return RedirectToAction(nameof(MisAlbums));
            }
        }

        // POST: Eliminar álbum
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<AlbumDto>.DeleteWithAuth(id, token);

                if (resultado)
                {
                    TempData["Success"] = "Álbum eliminado exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el álbum";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar el álbum: {ex.Message}";
            }

            return RedirectToAction(nameof(MisAlbums));
        }


        // GET: Buscar álbumes
        [AllowAnonymous]
        public async Task<IActionResult> Buscar(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var albums = await Crud<AlbumDto>.GetWithQuery("buscar", q);
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TerminoBusqueda = q;
                return View(albums ?? new List<AlbumDto>());
            }
            catch
            {
                TempData["Error"] = "Error en la búsqueda";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}