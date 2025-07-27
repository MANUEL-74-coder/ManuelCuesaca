using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Authorization;
using Melody.API.Services;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlaylistsCancionesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<PlaylistsCancionesController> _logger;

        public PlaylistsCancionesController(AppDbContext context, IUsuarioService usuarioService, ILogger<PlaylistsCancionesController> logger)
        {
            _context = context;
            _usuarioService = usuarioService;
            _logger = logger;
        }


        // POST: api/PlaylistsCanciones?playlistId=1&cancionId=5
        [HttpPost]
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult<PlaylistCancion>> AgregarCancionAPlaylist([FromQuery] int playlistId, [FromQuery] int cancionId)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                // Verificar que la playlist existe y es del usuario
                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.Id == playlistId && p.UsuarioId == usuario.Id);

                if (playlist == null)
                {
                    return NotFound("Playlist no encontrada o no tienes permisos para editarla");
                }

                // Verificar que la canción existe
                var cancion = await _context.Canciones
                    .Include(c => c.Artista)
                    .FirstOrDefaultAsync(c => c.Id == cancionId);

                if (cancion == null)
                {
                    return NotFound("Canción no encontrada");
                }

                // Verificar si la canción ya está en la playlist
                var relacionExistente = await _context.PlaylistsCanciones
                    .FirstOrDefaultAsync(pc => pc.PlaylistId == playlistId && pc.CancionId == cancionId);

                if (relacionExistente != null)
                {
                    return BadRequest("La canción ya está en esta playlist");
                }

                var playlistCancion = new PlaylistCancion
                {
                    PlaylistId = playlistId,
                    CancionId = cancionId
                };

                _context.PlaylistsCanciones.Add(playlistCancion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Canción {CancionTitulo} agregada a playlist {PlaylistNombre}",
                    cancion.Titulo, playlist.Nombre);

                return Ok(new
                {
                    mensaje = "Canción agregada a la playlist con éxito",
                    playlistCancionId = playlistCancion.Id,
                    playlistNombre = playlist.Nombre,
                    cancionTitulo = cancion.Titulo,
                    artistaNombre = cancion.Artista!.NombreArtista
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
        [Authorize(Roles = "userpremium,userfree")]
        public async Task<IActionResult> EliminarCancionDePlaylist(int id)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

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
                    cancionTitulo = playlistCancion.Cancion.Titulo,
                    playlistNombre = playlistCancion.Playlist.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar canción de playlist");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar canción de playlist");
            }
        }

        // GET: api/PlaylistsCanciones/verificar?playlistId=1&cancionId=5
        [HttpGet("verificar")]
        [Authorize(Roles = "userpremium,userfree")]
        public async Task<ActionResult> VerificarCancionEnPlaylist([FromQuery] int playlistId, [FromQuery] int cancionId)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                // Verificar que la playlist es del usuario
                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.Id == playlistId && p.UsuarioId == usuario.Id);

                if (playlist == null)
                {
                    return NotFound("Playlist no encontrada o no tienes permisos");
                }

                var playlistCancion = await _context.PlaylistsCanciones
                    .FirstOrDefaultAsync(pc => pc.PlaylistId == playlistId && pc.CancionId == cancionId);

                return Ok(new
                {
                    playlistId = playlistId,
                    cancionId = cancionId,
                    enPlaylist = playlistCancion != null,
                    playlistCancionId = playlistCancion?.Id
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