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

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PlaylistDto>>> ObtenerPlaylistsPublicas()
        {
            try
            {
                var playlistsBasicas = await _context.Playlists
                    .Where(p => p.EsPublica)
                    .ToListAsync();

                var resultado = new List<PlaylistDto>();

                foreach (var playlist in playlistsBasicas)
                {
                    try
                    {
                        var usuario = await _context.Usuarios.FindAsync(playlist.UsuarioId);

                        var totalCanciones = await _context.PlaylistsCanciones
                            .Where(pc => pc.PlaylistId == playlist.Id)
                            .CountAsync();

                        var playlistDto = new PlaylistDto
                        {
                            Id = playlist.Id,
                            Nombre = playlist.Nombre ?? "Sin nombre",
                            Imagen = !string.IsNullOrEmpty(playlist.Imagen)
                            ? playlist.Imagen
                            : "https://appmelody.blob.core.windows.net/playlists-images/default.jpg",
                            EsPublica = playlist.EsPublica,
                            TotalCanciones = totalCanciones,
                            CreadorId = usuario?.Id ?? 0,
                            CreadorNombre = usuario?.Nombre ?? "Usuario",
                            CreadorApellido = usuario?.Apellido ?? "Desconocido",
                            CreadorFotoPerfil = usuario?.FotoPerfil,
                            DuracionTotal = 0
                        };

                        resultado.Add(playlistDto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error procesando playlist {playlist.Id}");
                        continue;
                    }
                }

                return Ok(resultado.OrderByDescending(p => p.TotalCanciones));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener playlists públicas");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener playlists");
            }
        }

        // GET: api/Playlists/5 - CORREGIDO para usar CancionPlaylistDto
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<PlaylistDto>> ObtenerPlaylist(int id)
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
                if (!playlist.EsPublica)
                {
                    var usuarioActual = await _usuarioService.ObtenerUsuarioActualAsync();
                    if (playlist.UsuarioId != usuarioActual.Id)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, "No tienes permisos para ver esta playlist");
                    }
                }

                // Obtener canciones de la playlist CON ID de relación
                var canciones = await _context.PlaylistsCanciones
                    .Include(pc => pc.Cancion)
                    .ThenInclude(c => c.Artista)
                    .Include(pc => pc.Cancion)
                    .ThenInclude(c => c.Album)
                    .Include(pc => pc.Cancion)
                    .ThenInclude(c => c.Genero)
                    .Where(pc => pc.PlaylistId == id)
                    .Select(pc => new CancionPlaylistDto
                    {
                        PlaylistCancionId = pc.Id, // ¡IMPORTANTE! ID de la relación
                        CancionId = pc.Cancion!.Id,
                        CancionTitulo = pc.Cancion.Titulo,
                        ArchivoAudioUrl = pc.Cancion.ArchivoAudio,
                        PortadaUrl = pc.Cancion.PortadaUrl,
                        Duracion = pc.Cancion.Duracion,
                        ArtistaId = pc.Cancion.ArtistaId,
                        ArtistaNombre = pc.Cancion.Artista!.NombreArtista,
                        AlbumNombre = pc.Cancion.Album != null ? pc.Cancion.Album.Titulo : "Sin Álbum",
                        GeneroNombre = pc.Cancion.Genero!.Nombre,
                        FechaAgregada = DateTime.Now // Puedes agregar este campo a tu modelo si lo necesitas
                    })
                    .ToListAsync();

                var resultado = new PlaylistDto
                {
                    Id = playlist.Id,
                    Nombre = playlist.Nombre,
                    Imagen = playlist.Imagen,
                    EsPublica = playlist.EsPublica,
                    TotalCanciones = canciones.Count,
                    DuracionTotal = canciones.Sum(c => c.Duracion?.TotalMinutes ?? 0),
                    CreadorId = playlist.Usuario!.Id,
                    CreadorNombre = playlist.Usuario.Nombre,
                    CreadorApellido = playlist.Usuario.Apellido,
                    CreadorFotoPerfil = playlist.Usuario.FotoPerfil,
                    FechaCreacion = DateTime.Now, // O agregalo a tu modelo Playlist
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
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult<IEnumerable<PlaylistDto>>> ObtenerMisPlaylists()
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                var playlists = await _context.Playlists
                    .Include(p => p.PlaylistCanciones)
                    .Where(p => p.UsuarioId == usuario.Id)
                    .Select(p => new PlaylistDto
                    {
                        Id = p.Id,
                        Nombre = p.Nombre,
                        Imagen = p.Imagen,
                        EsPublica = p.EsPublica,
                        TotalCanciones = p.PlaylistCanciones != null ? p.PlaylistCanciones.Count : 0,
                        CreadorId = usuario.Id,
                        CreadorNombre = usuario.Nombre,
                        CreadorApellido = usuario.Apellido,
                        CreadorFotoPerfil = usuario.FotoPerfil,
                        DuracionTotal = 0 // Opcional: calcular si necesitas mostrar duración
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
        [Authorize(Roles = "userpremium")]
        public async Task<IActionResult> ActualizarPlaylist(int id, [FromForm] ActualizarPlaylistDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

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
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult> CrearPlaylist([FromForm] CrearPlaylistDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                string imagenUrl = null;

                // Subir imagen si se proporciona
                if (dto.Imagen != null)
                {
                    var (imagenValida, imagenError) = ValidacionService.ValidarArchivo(
                        dto.Imagen,
                        ValidacionService.Archivos.ExtensionesImagen,
                        ValidacionService.Archivos.MaxTamanoImagen);

                    if (!imagenValida)
                        return BadRequest(imagenError);

                    imagenUrl = await _blobService.SubirArchivoAsync(dto.Imagen, "Playlists", "playlist");
                }

                var playlist = new Playlist
                {
                    Nombre = dto.Nombre,
                    EsPublica = dto.EsPublica,
                    UsuarioId = usuario.Id,
                    Imagen = imagenUrl ?? "https://appmelody.blob.core.windows.net/playlists-images/default.jpg" // Imagen por defecto
                };

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
        [Authorize(Roles = "userpremium")]
        public async Task<IActionResult> EliminarPlaylist(int id)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

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
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PlaylistDto>>> BuscarPlaylists([FromQuery] string q)
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
                   .Select(p => new PlaylistDto
                   {
                       Id = p.Id,
                       Nombre = p.Nombre,
                       Imagen = p.Imagen,
                       EsPublica = p.EsPublica,
                       TotalCanciones = p.PlaylistCanciones != null ? p.PlaylistCanciones.Count : 0,
                       CreadorId = p.Usuario!.Id,
                       CreadorNombre = p.Usuario.Nombre,
                       CreadorApellido = p.Usuario.Apellido,
                       CreadorFotoPerfil = p.Usuario.FotoPerfil,
                       DuracionTotal = 0
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