using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Melody.Modelos.DTOs;
using Melody.API.Services;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAzureBlobService _blobService;
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<PlaylistsController> _logger;

        public PlaylistsController(AppDbContext context, IAzureBlobService blobService,
                                    IUsuarioService usuarioService, ILogger<PlaylistsController> logger)
        {
            _context = context;
            _blobService = blobService;
            _usuarioService = usuarioService;
            _logger = logger;
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

                // Verificar permisos
                var usuarioActual = await _usuarioService.ObtenerUsuarioActualAsync();
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
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();
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


        // PUT: api/Playlists/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> ActualizarPlaylist(int id, [FromForm] ActualizarPlaylistDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuario.Id);

                if (playlist == null)
                {
                    return NotFound("Playlist no encontrada o no tienes permisos para editarla");
                }

                // Actualizar imagen si se proporciona
                if (dto.Imagen != null)
                {
                    var (imagenValida, imagenError) = ValidacionService.ValidarArchivo(
                    dto.Imagen,
                    ValidacionService.Archivos.ExtensionesImagen,
                    ValidacionService.Archivos.MaxTamanoImagen);

                    if (!imagenValida)
                        return BadRequest(imagenError);

                    await _blobService.EliminarArchivoAsync(playlist.Imagen, "Playlists");
                    playlist.Imagen = await _blobService.SubirArchivoAsync(dto.Imagen, "Playlists", "playlist");
                }

                // Actualizar datos solo si se proporcionan
                if (!string.IsNullOrEmpty(dto.Nombre))
                    playlist.Nombre = dto.Nombre;

                if (dto.EsPublica.HasValue)
                    playlist.EsPublica = dto.EsPublica.Value;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Playlist actualizada con éxito",
                    playlist = new
                    {
                        playlist.Id,
                        playlist.Nombre,
                        playlist.Imagen,
                        playlist.EsPublica
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la playlist con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar la playlist");
            }
        }

        // POST: api/Playlists
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<object>> CrearPlaylist([FromForm] CrearPlaylistDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var playlist = new Playlist
                {
                    Nombre = dto.Nombre,
                    EsPublica = dto.EsPublica,
                    UsuarioId = usuario.Id
                };

                // Subir imagen si se proporciona
                if (dto.Imagen != null)
                {
                    var (imagenValida, imagenError) = ValidacionService.ValidarArchivo(
                    dto.Imagen,
                    ValidacionService.Archivos.ExtensionesImagen,
                    ValidacionService.Archivos.MaxTamanoImagen);

                    if (!imagenValida)
                        return BadRequest(imagenError);

                    playlist.Imagen = await _blobService.SubirArchivoAsync(dto.Imagen, "Playlists", "playlist");
                }

                _context.Playlists.Add(playlist);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Playlist creada: {Nombre} por {Usuario}", playlist.Nombre, usuario.Email);

                return Ok(new
                {
                    mensaje = "Playlist creada con éxito",
                    playlist = new
                    {
                        playlist.Id,
                        playlist.Nombre,
                        playlist.Imagen,
                        playlist.EsPublica
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la playlist");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear la playlist");
            }
        }

        // DELETE: api/Playlists/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> EliminarPlaylist(int id)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();
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

                // Eliminar imagen 
                await _blobService.EliminarArchivoAsync(playlist.Imagen, "Playlists");

                _context.Playlists.Remove(playlist);
                await _context.SaveChangesAsync();

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
    }
}