using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Melody.Modelos.DTOs;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly BlobContainerClient _playlistImagesContainer;
        private readonly ILogger<PlaylistsController> _logger;

        public PlaylistsController(AppDbContext context, UserManager<Usuario> userManager, IConfiguration configuration, ILogger<PlaylistsController> logger)
        {
            _context = context;
            _userManager = userManager;
            string playlistImagesSasUrl = configuration["AzureStorage:Playlists"];
            _playlistImagesContainer = new BlobContainerClient(new Uri(playlistImagesSasUrl));
            _logger = logger;
        }

        // Helper method to get the current user
        private async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _userManager.FindByIdAsync(userId.ToString());
        }

        // GET: api/Playlists
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerPlaylistsPublicas()
        {
            try
            {
                var playlists = await _context.Playlists
                    .Include(p => p.Usuario)
                    .Include(p => p.PlaylistCanciones)
                    .Where(p => p.EsPublica)
                    .Select(p => new
                    {
                        p.Id,
                        p.Nombre,
                        p.Imagen,
                        TotalCanciones = p.PlaylistCanciones != null ? p.PlaylistCanciones.Count : 0,
                        Creador = new
                        {
                            p.Usuario!.Id,
                            p.Usuario.Nombre,
                            p.Usuario.Apellido,
                            p.Usuario.FotoPerfil
                        }
                    })
                    .OrderByDescending(p => p.TotalCanciones)
                    .ToListAsync();

                return Ok(playlists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener playlists públicas");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener playlists");
            }
        }

        // GET: api/Playlists/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> ObtenerPlaylist(int id)
        {
            try
            {
                var playlist = await _context.Playlists
                    .Include(p => p.Usuario)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (playlist == null)
                {
                    return NotFound("Playlist no encontrada");
                }

                // Verificar permisos: debe ser pública o del usuario actual
                var usuarioActual = await ObtenerUsuarioActualAsync();
                if (!playlist.EsPublica && (usuarioActual == null || playlist.UsuarioId != usuarioActual.Id))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "No tienes permisos para ver esta playlist");
                }

                // Obtener canciones de la playlist
                var canciones = await _context.PlaylistsCanciones
                    .Include(pc => pc.Cancion)
                    .ThenInclude(c => c.Artista)
                    .Include(pc => pc.Cancion)
                    .ThenInclude(c => c.Genero)
                    .Where(pc => pc.PlaylistId == id)
                    .Select(pc => new
                    {
                        PlaylistCancionId = pc.Id,
                        Cancion = new
                        {
                            pc.Cancion!.Id,
                            pc.Cancion.Titulo,
                            pc.Cancion.Duracion,
                            pc.Cancion.PortadaUrl,
                            pc.Cancion.ArchivoAudio,
                            Artista = new
                            {
                                pc.Cancion.Artista!.Id,
                                pc.Cancion.Artista.NombreArtista
                            },
                            Genero = pc.Cancion.Genero!.Nombre
                        }
                    })
                    .ToListAsync();

                var resultado = new
                {
                    playlist.Id,
                    playlist.Nombre,
                    playlist.Imagen,
                    playlist.EsPublica,
                    TotalCanciones = canciones.Count,
                    DuracionTotal = canciones.Sum(c => c.Cancion.Duracion?.TotalMinutes ?? 0),
                    Creador = new
                    {
                        playlist.Usuario!.Id,
                        playlist.Usuario.Nombre,
                        playlist.Usuario.Apellido,
                        playlist.Usuario.FotoPerfil
                    },
                    Canciones = canciones
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la playlist con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener la playlist");
            }
        }


        // GET: api/Playlists/mis-playlists - Obtener mis playlists
        [HttpGet("mis-playlists")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerMisPlaylists()
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var playlists = await _context.Playlists
                    .Include(p => p.PlaylistCanciones)
                    .Where(p => p.UsuarioId == usuario.Id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Nombre,
                        p.Imagen,
                        p.EsPublica,
                        TotalCanciones = p.PlaylistCanciones != null ? p.PlaylistCanciones.Count : 0
                    })
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                return Ok(playlists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las playlists del usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener playlists");
            }
        }


        // DELETE: api/Playlists/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> EliminarPlaylist(int id)
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var playlist = await _context.Playlists
                    .Include(p => p.PlaylistCanciones)
                    .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuario.Id);

                if (playlist == null)
                {
                    return NotFound("Playlist no encontrada o no tienes permisos para eliminarla");
                }

                // Eliminar imagen si existe
                if (!string.IsNullOrEmpty(playlist.Imagen))
                {
                    await EliminarArchivoBlobAsync(playlist.Imagen);
                }

                _context.Playlists.Remove(playlist);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Playlist eliminada: {Nombre} por {Usuario}", playlist.Nombre, usuario.Email);

                return Ok(new { mensaje = "Playlist eliminada con éxito" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la playlist con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar la playlist");
            }
        }

        // GET: api/Playlists/buscar?q=rock - Buscar playlists públicas
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarPlaylists([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var playlists = await _context.Playlists
                    .Include(p => p.Usuario)
                    .Include(p => p.PlaylistCanciones)
                    .Where(p => p.EsPublica && p.Nombre.Contains(q))
                    .Select(p => new
                    {
                        p.Id,
                        p.Nombre,
                        p.Imagen,
                        TotalCanciones = p.PlaylistCanciones != null ? p.PlaylistCanciones.Count : 0,
                        Creador = p.Usuario!.Nombre + " " + p.Usuario.Apellido
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(playlists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar playlists con término '{Termino}'", q);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error en la búsqueda");
            }
        }
        // Métodos auxiliares para manejo de archivos
        private async Task<string> SubirImagenPlaylistAsync(IFormFile imagen)
        {
            var extension = Path.GetExtension(imagen.FileName).ToLower();
            var nombreImagen = $"playlist_{Guid.NewGuid()}{extension}";
            var blobClient = _playlistImagesContainer.GetBlobClient(nombreImagen);

            using (var stream = imagen.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            return $"https://appmelody.blob.core.windows.net/playlist-images/{nombreImagen}";
        }

        private async Task EliminarArchivoBlobAsync(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var uri = new Uri(url);
                    var blobName = uri.Segments.Last();
                    var blobClient = _playlistImagesContainer.GetBlobClient(blobName);
                    await blobClient.DeleteIfExistsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al eliminar archivo blob: {Url}", url);
                }
            }
        }
    }
}