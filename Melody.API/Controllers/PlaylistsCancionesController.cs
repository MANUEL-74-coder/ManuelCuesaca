using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Melody.Modelos.DTOs;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistsCancionesController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<PlaylistsCancionesController> _logger;

        public PlaylistsCancionesController(AppDbContext context, UserManager<Usuario> userManager, ILogger<PlaylistsCancionesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Helper method para obtener usuario actual
        private async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _userManager.FindByIdAsync(userId.ToString());
        }


        // POST: api/PlaylistsCanciones
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<object>> AgregarCancionAPlylist([FromBody] AgregarCancionPlaylistDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                // Verificar que la playlist existe y es del usuario
                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.Id == dto.PlaylistId && p.UsuarioId == usuario.Id);

                if (playlist == null)
                {
                    return NotFound("Playlist no encontrada o no tienes permisos para editarla");
                }

                // Verificar que la canción existe
                var cancion = await _context.Canciones
                    .Include(c => c.Artista)
                    .FirstOrDefaultAsync(c => c.Id == dto.CancionId);

                if (cancion == null)
                {
                    return NotFound("Canción no encontrada");
                }

                // Verificar si la canción ya está en la playlist
                var relacionExistente = await _context.PlaylistsCanciones
                    .FirstOrDefaultAsync(pc => pc.PlaylistId == dto.PlaylistId && pc.CancionId == dto.CancionId);

                if (relacionExistente != null)
                {
                    return BadRequest("La canción ya está en esta playlist");
                }

                var playlistCancion = new PlaylistCancion
                {
                    PlaylistId = dto.PlaylistId,
                    CancionId = dto.CancionId
                };

                _context.PlaylistsCanciones.Add(playlistCancion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Canción {CancionTitulo} agregada a playlist {PlaylistNombre}",
                    cancion.Titulo, playlist.Nombre);

                return Ok(new
                {
                    mensaje = "Canción agregada a la playlist con éxito",
                    playlistCancion = new
                    {
                        playlistCancion.Id,
                        Playlist = new { playlist.Id, playlist.Nombre },
                        Cancion = new
                        {
                            cancion.Id,
                            cancion.Titulo,
                            Artista = cancion.Artista!.NombreArtista
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar canción a playlist");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al agregar canción a playlist");
            }
        }

        // DELETE: api/PlaylistsCanciones/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> EliminarCancionDePlaylist(int id)
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var playlistCancion = await _context.PlaylistsCanciones
                    .Include(pc => pc.Playlist)
                    .Include(pc => pc.Cancion)
                    .ThenInclude(c => c.Artista)
                    .FirstOrDefaultAsync(pc => pc.Id == id);

                if (playlistCancion == null)
                {
                    return NotFound("Relación playlist-canción no encontrada");
                }

                // Verificar que la playlist es del usuario
                if (playlistCancion.Playlist!.UsuarioId != usuario.Id)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "No tienes permisos para editar esta playlist");
                }

                _context.PlaylistsCanciones.Remove(playlistCancion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Canción {CancionTitulo} eliminada de playlist {PlaylistNombre}",
                    playlistCancion.Cancion!.Titulo, playlistCancion.Playlist.Nombre);

                return Ok(new
                {
                    mensaje = "Canción eliminada de la playlist con éxito",
                    cancion = playlistCancion.Cancion.Titulo,
                    playlist = playlistCancion.Playlist.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar canción de playlist");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar canción de playlist");
            }
        }
        // GET: api/PlaylistCanciones/verificar?playlistId=1&cancionId=5 - Verificar si canción está en playlist
        [HttpGet("verificar")]
        [Authorize]
        public async Task<ActionResult<object>> VerificarCancionEnPlaylist([FromQuery] int playlistId, [FromQuery] int cancionId)
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                // Verificar que la playlist es del usuario
                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.Id == playlistId && p.UsuarioId == usuario.Id);

                if (playlist == null)
                {
                    return NotFound("Playlist no encontrada o no tienes permisos");
                }

                var enPlaylist = await _context.PlaylistsCanciones
                    .AnyAsync(pc => pc.PlaylistId == playlistId && pc.CancionId == cancionId);

                return Ok(new
                {
                    playlistId = playlistId,
                    cancionId = cancionId,
                    enPlaylist = enPlaylist
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar canción en playlist");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al verificar canción en playlist");
            }
        }
    }
}