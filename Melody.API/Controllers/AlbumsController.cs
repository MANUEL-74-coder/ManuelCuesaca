using Melody.Modelos.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Melody.API.Services;
using Humanizer;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAzureBlobService _blobService;
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<AlbumsController> _logger;

        public AlbumsController(AppDbContext context, IAzureBlobService blobService,
                                IUsuarioService usuarioService, ILogger<AlbumsController> logger)
        {
            _context = context;
            _blobService = blobService;
            _usuarioService = usuarioService;
            _logger = logger;
        }
        // GET: api/Albums
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AlbumDto>>> ObtenerAlbums()
        {
            try
            {
                var albums = await _context.Albums
                    .Include(a => a.Artista)
                    .Include(a => a.Genero)
                    .Include(a => a.Canciones)
                    .Select(a => new AlbumDto
                    {
                        Id = a.Id,
                        Titulo = a.Titulo,
                        FechaLanzamiento = a.FechaLanzamiento,
                        PortadaUrl = a.PortadaUrl,
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

        // GET: api/Albums/mis-albums
        [HttpGet("mis-albums")]
        [Authorize(Roles = "artista")]
        public async Task<ActionResult<IEnumerable<Album>>> ObtenerMisAlbums()
        {
            try
            {
                var artista = await _usuarioService.ObtenerArtistaActualAsync();

                var albums = await _context.Albums
                    .Include(a => a.Artista)
                    .Include(a => a.Genero)
                    .Include(a => a.Canciones)
                    .Where(a => a.ArtistaId == artista.Id)
                    .OrderByDescending(a => a.FechaLanzamiento)
                    .ToListAsync();

                return Ok(albums);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los albums del artista actual");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los albums");
            }
        }


        // GET: api/Albums/5 - ACTUALIZADO con EsFavorito
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<AlbumDto>> ObtenerAlbum(int id)
        {
            try
            {
                // Obtener usuario actual para verificar favoritos
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();
                var usuarioId = usuario?.Id;
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
                    .Include(c => c.Artista)
                    .Include(c => c.Album)
                    .Include(c => c.Genero)
                    .Where(c => c.AlbumId == id)
                    .Select(c => new CancionDto
                    {
                        Id = c.Id,
                        Titulo = c.Titulo,
                        Duracion = c.Duracion,
                        PortadaUrl = c.PortadaUrl,
                        FechaLanzamiento = c.FechaLanzamiento,
                        ArchivoAudioUrl = c.ArchivoAudio,
                        ArtistaId = c.ArtistaId,
                        ArtistaNombre = c.Artista!.NombreArtista,
                        AlbumNombre = c.Album!.Titulo,
                        GeneroNombre = c.Genero!.Nombre,
                        EsFavorito = usuarioId.HasValue &&
                                   _context.MeGustas.Any(mg => mg.UsuarioId == usuarioId.Value && mg.CancionId == c.Id)
                    })
                    .OrderBy(c => c.FechaLanzamiento)
                    .ToListAsync();
                var resultado = new AlbumDto
                {
                    Id = album.Id,
                    Titulo = album.Titulo,
                    FechaLanzamiento = album.FechaLanzamiento,
                    PortadaUrl = album.PortadaUrl,
                    ArtistaId = album.Artista!.Id,
                    NombreArtista = album.Artista.NombreArtista,
                    GeneroId = album.GeneroId,
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
                var artista = await _usuarioService.ObtenerArtistaActualAsync();

                var albumExistente = await _context.Albums
                    .FirstOrDefaultAsync(a => a.Id == id && a.ArtistaId == artista.Id);
                if (albumExistente == null)
                {
                    return NotFound("Album no encontrado o no pertenece al artista actual.");
                }
                //Actualizar portada si se proporciona una nueva
                if (album.Portada != null)
                {
                    var (imagenValida, imagenError) = ValidacionService.ValidarArchivo(
                    album.Portada,
                    ValidacionService.Archivos.ExtensionesImagen,
                    ValidacionService.Archivos.MaxTamanoImagen);

                    if (!imagenValida)
                        return BadRequest(imagenError);

                    await _blobService.EliminarArchivoAsync(albumExistente.PortadaUrl, "Albums");
                    albumExistente.PortadaUrl = await _blobService.SubirArchivoAsync(album.Portada, "Albums", "album");
                }
                //Actualizar los campos del album
                if (album.Titulo != null)
                    albumExistente.Titulo = album.Titulo;
                if (album.GeneroId > 0)
                    albumExistente.GeneroId = album.GeneroId;
                await _context.SaveChangesAsync();

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

        // POST: api/Albums/subir-album
        [HttpPost("subir-album")]
        [Authorize(Roles = "artista")]
        public async Task<ActionResult<Album>> CrearAlbum([FromForm] CrearAlbumDto album)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var artista = await _usuarioService.ObtenerArtistaActualAsync();

                string portadaUrl = null;

                // Subir portada si se proporciona
                if (album.Portada != null)
                {
                    var (imagenValida, imagenError) = ValidacionService.ValidarArchivo(
                        album.Portada,
                        ValidacionService.Archivos.ExtensionesImagen,
                        ValidacionService.Archivos.MaxTamanoImagen);

                    if (!imagenValida)
                        return BadRequest(imagenError);

                    portadaUrl = await _blobService.SubirArchivoAsync(album.Portada, "Albums", "album");
                }
                var nuevoAlbum = new Album
                {
                    Titulo = album.Titulo,
                    FechaLanzamiento = DateTime.Now,
                    GeneroId = album.GeneroId,
                    ArtistaId = artista.Id,
                    PortadaUrl = portadaUrl ?? "https://appmelody.blob.core.windows.net/album-images/default.jpg" // Valor por defecto
                };

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
        // PUT: api/Albums/agregar-cancion/5
        [HttpPut("agregar-cancion/{id}")]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> AgregarCancion(int id, [FromBody] int cancionId)
        {
            try
            {
                var artista = await _usuarioService.ObtenerArtistaActualAsync();

                var album = await _context.Albums.FindAsync(id);
                if (album == null || album.ArtistaId != artista.Id)
                    return NotFound("Álbum no encontrado");

                var cancion = await _context.Canciones.FindAsync(cancionId);
                if (cancion == null || cancion.ArtistaId != artista.Id)
                    return BadRequest("Canción no válida");

                // Verificar que la canción no tenga álbum
                if (cancion.AlbumId != null)
                    return BadRequest("La canción ya pertenece a un álbum");

                cancion.AlbumId = id;
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Canción agregada al álbum" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error al agregar canción al álbum");
            }
        }
        // DELETE: api/Albums/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            try
            {
                var artista = await _usuarioService.ObtenerArtistaActualAsync();

                var album = await _context.Albums
                    .Include(a => a.Canciones)
                    .FirstOrDefaultAsync(a => a.Id == id && a.ArtistaId == artista.Id);

                if (album == null)
                {
                    return NotFound("Album no encontrado o no pertenece al artista actual.");
                }

                if (album.Canciones?.Any() == true)
                {
                    foreach (var cancion in album.Canciones)
                    {
                        cancion.AlbumId = null; // ← Las canciones quedan como "sueltas"
                    }
                    _context.Canciones.UpdateRange(album.Canciones);
                }

                //Eliminar la portada del Blob Storage si existe
                await _blobService.EliminarArchivoAsync(album.PortadaUrl, "Albums");

                _context.Albums.Remove(album);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Álbum eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el album con ID {AlbumId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar el album");
            }
        }
        // GET: api/Albums/buscar?q=titulo -Buscar álbums
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<AlbumDto>>> BuscarAlbums([FromQuery] string q)
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
                    .Select(a => new AlbumDto
                    {
                        Id = a.Id,
                        Titulo = a.Titulo,
                        PortadaUrl = a.PortadaUrl,
                        FechaLanzamiento = a.FechaLanzamiento,
                        NombreArtista = a.Artista!.NombreArtista,
                        GeneroNombre = a.Genero!.Nombre,
                        TotalCanciones = a.Canciones != null ? a.Canciones.Count : 0
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
    }
}
