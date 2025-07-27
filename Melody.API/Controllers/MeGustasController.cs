using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Melody.API.Services;
using Melody.Modelos.DTOs;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "userfree,userpremium")]
    public class MeGustaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<MeGustaController> _logger;

        public MeGustaController(AppDbContext context, IUsuarioService usuarioService,
                                ILogger<MeGustaController> logger)
        {
            _context = context;
            _usuarioService = usuarioService;
            _logger = logger;
        }
        // POST: api/MeGusta/toggle/{cancionId} - Toggle agregar/quitar de favoritos
        [HttpPost("toggle/{cancionId}")]
        public async Task<ActionResult<object>> ToggleMeGusta(int cancionId)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                // Verificamos que la canción existe
                var cancion = await _context.Canciones
                    .Include(c => c.Artista)
                    .FirstOrDefaultAsync(c => c.Id == cancionId);

                if (cancion == null)
                    return NotFound("Canción no encontrada");

                // Buscar si ya existe el Me Gusta
                var meGustaExistente = await _context.MeGustas
                    .FirstOrDefaultAsync(mg => mg.UsuarioId == usuario.Id && mg.CancionId == cancionId);

                if (meGustaExistente != null)
                {
                    // Ya está en favoritos, entonces quitar
                    _context.MeGustas.Remove(meGustaExistente);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        accion = "quitar_favorito",
                        mensaje = "Canción quitada de favoritos",
                        esFavorito = false,
                        cancion = new
                        {
                            cancion.Id,
                            cancion.Titulo,
                            cancion.Artista.NombreArtista
                        }
                    });
                }
                else
                {
                    // No está en favoritos, entonces agregar
                    var nuevoMeGusta = new MeGusta
                    {
                        UsuarioId = usuario.Id,
                        CancionId = cancionId,
                        FechaAgregado = DateTime.Now
                    };

                    _context.MeGustas.Add(nuevoMeGusta);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        accion = "agregar_favorito",
                        mensaje = "Canción agregada a favoritos",
                        esFavorito = true,
                        cancion = new
                        {
                            cancion.Id,
                            cancion.Titulo,
                            cancion.Artista.NombreArtista
                        },
                        fechaAgregado = nuevoMeGusta.FechaAgregado
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al toggle Me Gusta");
                return StatusCode(500, "Error al procesar favorito");
            }
        }
        // GET: api/MeGusta/mis-favoritos - Ver todas mis canciones favoritas
        [HttpGet("mis-favoritos")]
        [Authorize(Roles = "userfree,userpremium")]
        public async Task<ActionResult<IEnumerable<MeGustaDto>>> ObtenerMisFavoritos()
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                var favoritos = await _context.MeGustas
                    .Include(mg => mg.Cancion)
                    .ThenInclude(c => c.Artista)
                    .Include(mg => mg.Cancion)
                    .ThenInclude(c => c.Album)
                    .Include(mg => mg.Cancion)
                    .ThenInclude(c => c.Genero)
                    .Where(mg => mg.UsuarioId == usuario.Id)
                    .Select(mg => new MeGustaDto
                    {
                        Id = mg.Id,
                        UsuarioId = mg.UsuarioId,
                        CancionId = mg.CancionId,
                        FechaAgregado = mg.FechaAgregado,
                        CancionTitulo = mg.Cancion!.Titulo,
                        ArchivoAudioUrl = mg.Cancion.ArchivoAudio,
                        PortadaUrl = mg.Cancion.PortadaUrl,
                        Duracion = mg.Cancion.Duracion,
                        ArtistaId = mg.Cancion.ArtistaId,
                        ArtistaNombre = mg.Cancion.Artista!.NombreArtista,
                        AlbumNombre = mg.Cancion.Album != null ? mg.Cancion.Album.Titulo : "Sin Álbum",
                        AlbumId = mg.Cancion.AlbumId,
                        GeneroNombre = mg.Cancion.Genero!.Nombre
                    })
                    .OrderByDescending(mg => mg.FechaAgregado)
                    .ToListAsync();

                return Ok(favoritos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener canciones favoritas");
                return StatusCode(500, "Error al obtener canciones favoritas");
            }
        }
    }
}