using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Melody.Modelos.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<ArtistasController> _logger;
        private readonly BlobContainerClient _albumsContainer;

        public AlbumsController(AppDbContext context, UserManager<Usuario> userManager, IConfiguration configuration, ILogger<ArtistasController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;

            string albumsSasUrl = configuration["AzureStorage:Albums"];
            _albumsContainer = new BlobContainerClient(new Uri(albumsSasUrl));
        }
        private async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _userManager.FindByIdAsync(userId.ToString());
        }

        // GET: api/Albums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Album>>> ObtenerAlbums()
        {
            try
            {
                var albums = await _context.Albums
                    .Include(a => a.Artista)
                    .Include(a => a.Genero)
                    .Include(a => a.Canciones)
                    .Select(a => new
                    {
                        a.Id,
                        a.Titulo,
                        a.FechaLanzamiento,
                        a.PortadaUrl,
                        ArtistaId = a.Artista!.Id,
                        NombreArtista = a.Artista.NombreArtista,
                        GeneroNombre = a.Genero!.Nombre,
                        TotalCanciones = a.Canciones != null ? a.Canciones.Count : 0

                    })
                    .OrderByDescending(a => a.FechaLanzamiento)
                    .ToListAsync();
                return Ok(albums);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los albums");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los albums");
            }
        }

        // GET: api/Albums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Album>> ObtenerAlbum(int id)
        {
            try
            {
                var album = await _context.Albums
                    .Include(a => a.Artista)
                    .Include(a => a.Genero)
                    .Include(a => a.Canciones)
                    .FirstOrDefaultAsync(a => a.Id == id);
                if (album == null)
                {
                    return NotFound();
                }
                var canciones = await _context.Canciones
                    .Where(c => c.AlbumId == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Titulo,
                        c.Duracion,
                        c.PortadaUrl,
                        c.FechaLanzamiento
                    })
                    .OrderBy(c => c.FechaLanzamiento)
                    .ToListAsync();
                var resultado = new
                {
                    album.Id,
                    album.Titulo,
                    album.FechaLanzamiento,
                    album.PortadaUrl,
                    Artista = new
                    {
                        album.Artista!.Id,
                        album.Artista.NombreArtista,
                        album.Artista.ImagenPerfil
                    },
                    GeneroNombre = album.Genero!.Nombre,
                    TotalCanciones = canciones.Count,
                    Canciones = canciones
                };
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el album con ID {AlbumId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el album");
            }
        }

        // PUT: api/Albums/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> ActualizarAlbum(int id, [FromForm] ActualizarAlbumDto album)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var usuarioActual = await ObtenerUsuarioActualAsync();
                if (usuarioActual == null)
                {
                    return Unauthorized();
                }
                var artista = await _context.Artistas
                    .FirstOrDefaultAsync(a => a.UsuarioId == usuarioActual.Id);
                if (artista == null)
                {
                    return BadRequest("El usuario actual no tiene un artista asociado.");
                }
                var albumExistente = await _context.Albums
                    .FirstOrDefaultAsync(a => a.Id == id && a.ArtistaId == artista.Id);
                if (albumExistente == null)
                {
                    return NotFound("Album no encontrado o no pertenece al artista actual.");
                }
                //Actualizar portada si se proporciona una nueva
                if (album.Portada != null)
                {
                    var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(album.Portada.FileName).ToLowerInvariant();
                    if (!extensionesPermitidas.Contains(extension))
                    {
                        return BadRequest("La portada debe ser una imagen JPG o PNG.");
                    }
                    if (album.Portada.Length > 5 * 1024 * 1024) // 5 MB
                    {
                        return BadRequest("La portada no puede exceder los 5 MB.");
                    }
                    //Eliminar la portada anterior si existe
                    if (!string.IsNullOrEmpty(albumExistente.PortadaUrl))
                    {
                        await EliminarArchivoBobAsync(albumExistente.PortadaUrl);
                    }
                    //Subir la nueva portada
                    albumExistente.PortadaUrl = await SubirPortadaAsync(album.Portada);
                }
                //Actualizar los campos del album
                if (album.Titulo != null)
                    albumExistente.Titulo = album.Titulo;
                if (album.GeneroId > 0)
                    albumExistente.GeneroId = album.GeneroId;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Album actualizado exitosamente: {AlbumId}", albumExistente.Id);
                return Ok(new
                {
                    mensaje = "Album actualizado exitosamente",
                    album = new
                    {
                        albumExistente.Id,
                        albumExistente.Titulo,
                        albumExistente.FechaLanzamiento,
                        albumExistente.PortadaUrl,
                    }
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el album");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el album");
            }
        }

        // POST: api/Albums
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "artista")]
        public async Task<ActionResult<Album>> CrearAlbum([FromForm] CrearAlbumDto album)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var usuarioActual = await ObtenerUsuarioActualAsync();
                if (usuarioActual == null)
                {
                    return Unauthorized();
                }
                var artista = await _context.Artistas
                    .FirstOrDefaultAsync(a => a.UsuarioId == usuarioActual.Id);
                if (artista == null)
                {
                    return BadRequest("El usuario actual no tiene un artista asociado.");
                }
                var nuevoAlbum = new Album
                {
                    Titulo = album.Titulo,
                    FechaLanzamiento = DateTime.Now,
                    GeneroId = album.GeneroId,
                    ArtistaId = artista.Id
                };
                // subir portada si se proporciona
                if (album.Portada != null)
                {
                    var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(album.Portada.FileName).ToLowerInvariant();
                    if (!extensionesPermitidas.Contains(extension))
                    {
                        return BadRequest("La portada debe ser una imagen JPG o PNG.");
                    }
                    if (album.Portada.Length > 5 * 1024 * 1024) // 5 MB
                    {
                        return BadRequest("La portada no puede exceder los 5 MB.");
                    }
                    nuevoAlbum.PortadaUrl = await SubirPortadaAsync(album.Portada);
                }
                _context.Albums.Add(nuevoAlbum);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Album creado exitosamente: {AlbumId}", nuevoAlbum.Id);
                return Ok(new
                {
                    mensaje = "Álbum creado con éxito",
                    album = new
                    {
                        nuevoAlbum.Id,
                        nuevoAlbum.Titulo,
                        nuevoAlbum.FechaLanzamiento,
                        nuevoAlbum.PortadaUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el album");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear el album");
            }
        }

        // DELETE: api/Albums/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized();
                }
                var artista = await _context.Artistas
                    .FirstOrDefaultAsync(a => a.UsuarioId == usuario.Id);
                if (artista == null)
                {
                    return BadRequest("El usuario actual no tiene un artista asociado.");
                }
                var album = await _context.Albums
                    .FirstOrDefaultAsync(a => a.Id == id && a.ArtistaId == artista.Id);
                if (album == null)
                {
                    return NotFound("Album no encontrado o no pertenece al artista actual.");
                }
                //Eliminar la portada del Blob Storage si existe
                if (!string.IsNullOrEmpty(album.PortadaUrl))
                {
                    await EliminarArchivoBobAsync(album.PortadaUrl);
                }
                _context.Albums.Remove(album);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Album eliminado exitosamente: {AlbumId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el album con ID {AlbumId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar el album");
            }
        }
        // GET: api/Albums/buscar?q=titulo -Buscar álbums
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarAlbums([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("El parámetro de búsqueda no puede estar vacío.");
            }
            try
            {
                var albums = await _context.Albums
                    .Include(a => a.Artista)
                    .Where(a => a.Titulo.Contains(q) || a.Artista!.NombreArtista.Contains(q))
                    .Select(a => new
                    {
                        a.Id,
                        a.Titulo,
                        a.PortadaUrl,
                        a.FechaLanzamiento,
                        NombreArtista = a.Artista!.NombreArtista
                    })
                    .Take(20)
                    .ToListAsync();
                return Ok(albums);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar albums");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al buscar albums");
            }
        }

        private async Task<string> SubirPortadaAsync(IFormFile portada)
        {
            var extension = Path.GetExtension(portada.FileName).ToLower();
            var nombrePortada = $"album_{Guid.NewGuid()}{extension}";
            var blobClient = _albumsContainer.GetBlobClient(nombrePortada);
            using (var stream = portada.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }
            return $"https://appmelody.blob.core.windows.net/album-images/{nombrePortada}";
        }

        private async Task EliminarArchivoBobAsync(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var uri = new Uri(url);
                    var blobName = Path.GetFileName(new Uri(url).LocalPath);
                    var blobClient = _albumsContainer.GetBlobClient(blobName);
                    await blobClient.DeleteIfExistsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al eliminar el archivo de Blob Storage: {BlobUrl}", url);
                }
            }
        }

        private bool AlbumExists(int id)
        {
            return _context.Albums.Any(e => e.Id == id);
        }
    }
}