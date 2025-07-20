using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Melody.Modelos.DTOs;
using Microsoft.AspNetCore.Authorization;
using Melody.API.Services;
using Humanizer;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CancionesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAzureBlobService _blobService;
        private readonly IUsuarioService _usuarioService;
        private readonly IAudioService _audioService;
        private readonly ILogger<CancionesController> _logger;

        public CancionesController(AppDbContext context, IAzureBlobService blobService,
                                    IUsuarioService usuarioService,
                                    IAudioService audioService,
                                    ILogger<CancionesController> logger)
        {
            _context = context;
            _logger = logger;
            _blobService = blobService;
            _audioService = audioService;
            _usuarioService = usuarioService;
        }

        // GET: api/Canciones
        [HttpGet]
        public async Task<ActionResult> ObtenerCanciones()
        {
            try
            {
                var canciones = await _context.Canciones
                    .Include(c => c.Artista)
                    .Select(c => new
                    {
                        c.Id,
                        c.Titulo,
                        ArchivoAudioUrl = c.ArchivoAudio,
                        PortadaUrl = c.PortadaUrl,
                        ArtistaNombre = c.Artista!.NombreArtista
                    })
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();
                return Ok(canciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las canciones");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener las canciones");
            }
        }

        // GET: api/Canciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult> ObtenerCancion(int id)
        {
            try
            {
                var cancion = await _context.Canciones
                    .Include(c => c.Artista)
                    .Include(c => c.Genero)
                    .Include(c => c.Album)
                    .Where(c => c.Id == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Titulo,
                        c.FechaLanzamiento,
                        ArchivoAudioUrl = c.ArchivoAudio,
                        PortadaUrl = c.PortadaUrl,
                        c.Duracion,
                        c.GeneroId,
                        GeneroNombre = c.Genero!.Nombre,
                        c.AlbumId,
                        AlbumNombre = c.Album != null ? c.Album.Titulo : "Sin Álbum",
                        c.ArtistaId,
                        ArtistaNombre = c.Artista!.NombreArtista
                    })
                    .FirstOrDefaultAsync();
                if (cancion == null)
                {
                    return NotFound();
                }
                return Ok(cancion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la canción con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener la canción");
            }
        }
        // PUT: api/Canciones/agregar-album/5
        [HttpPut("agregar-album/{id}")]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> AgregarAAlbum(int id, [FromBody] int albumId)
        {
            try
            {
                var artista = await _usuarioService.ObtenerArtistaActualAsync();
                if (artista == null) return BadRequest("Usuario no es artista");

                var cancion = await _context.Canciones.FindAsync(id);
                if (cancion == null || cancion.ArtistaId != artista.Id)
                    return NotFound("Canción no encontrada");

                var album = await _context.Albums.FindAsync(albumId);
                if (album == null || album.ArtistaId != artista.Id)
                    return BadRequest("Álbum no válido");

                cancion.AlbumId = albumId;
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Canción agregada al álbum" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error al agregar canción al álbum");
            }
        }

        // PUT: api/Canciones/quitar-album/5
        [HttpPut("quitar-album/{id}")]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> QuitarDeAlbum(int id)
        {
            try
            {
                var artista = await _usuarioService.ObtenerArtistaActualAsync();
                if (artista == null) return BadRequest("Usuario no es artista");

                var cancion = await _context.Canciones.FindAsync(id);
                if (cancion == null || cancion.ArtistaId != artista.Id)
                    return NotFound("Canción no encontrada");

                cancion.AlbumId = null;
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Canción quitada del álbum" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error al quitar canción del álbum");
            }
        }

        // PUT: api/Canciones/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> ActualizarCancion(int id, [FromForm] ActualizarCancionDto cancion)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var artista = await _usuarioService.ObtenerArtistaActualAsync();
                if (artista == null)
                {
                    return BadRequest("El usuario no es un artista válido");
                }
                var cancionExistente = await _context.Canciones.FindAsync(id);
                if (cancionExistente == null || cancionExistente.ArtistaId != artista.Id)
                {
                    return NotFound("Canción no encontrada o sin permisos");
                }

                //Actualizar archivo de audio si se proporciona uno nuevo
                if (cancion.ArchivoAudio != null)
                {
                    var (audioValido, audioError) = ValidacionService.ValidarArchivo(
                    cancion.ArchivoAudio,
                    ValidacionService.Archivos.ExtensionesAudio,
                    ValidacionService.Archivos.MaxTamanoAudio);

                    if (!audioValido)
                        return BadRequest(audioError);

                    await _blobService.EliminarArchivoAsync(cancionExistente.ArchivoAudio, "Canciones");
                    cancionExistente.ArchivoAudio = await _blobService.SubirArchivoAsync(cancion.ArchivoAudio, "Canciones", "audio");
                    cancionExistente.Duracion = _audioService.ObtenerDuracionAudio(cancion.ArchivoAudio);
                }
                //Actualizar imagen de portada si se proporciona una nueva
                if (cancion.ImagenPortada != null)
                {
                    var (imagenValida, imagenError) = ValidacionService.ValidarArchivo(
                        cancion.ImagenPortada,
                        ValidacionService.Archivos.ExtensionesImagen,
                        ValidacionService.Archivos.MaxTamanoImagen);

                    if (!imagenValida)
                        return BadRequest(imagenError);

                    await _blobService.EliminarArchivoAsync(cancionExistente.PortadaUrl, "Portadas");
                    cancionExistente.PortadaUrl = await _blobService.SubirArchivoAsync(cancion.ImagenPortada, "Portadas", "portada");
                }
                //Actualizar otros campos
                cancionExistente.Titulo = cancion.Titulo;
                cancionExistente.GeneroId = cancion.GeneroId;
                cancionExistente.AlbumId = cancion.AlbumId;
                cancionExistente.FechaLanzamiento = cancion.FechaLanzamiento;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Canción actualizada con éxito: {Titulo}", cancionExistente.Titulo);
                return Ok(new
                {
                    mensaje = "Canción actualizada con éxito",
                    cancionId = cancionExistente.Id,
                    titulo = cancionExistente.Titulo
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la canción con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar la canción");
            }
        }

        // POST: api/canciones/subir
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("subir")]
        [Authorize(Roles = "artista")]
        public async Task<ActionResult> SubircCancion([FromForm] CancionCrearDto cancion)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var artista = await _usuarioService.ObtenerArtistaActualAsync();
                if (artista == null)
                {
                    return BadRequest("El usuario no es un artista válido");
                }

                //Validar archivo de audio
                var (audioValido, audioError) = ValidacionService.ValidarArchivo(
                    cancion.ArchivoAudio,
                    ValidacionService.Archivos.ExtensionesAudio,
                    ValidacionService.Archivos.MaxTamanoAudio);
                if (!audioValido)
                {
                    return BadRequest(audioError);
                }

                //Validar imagen de portada si se proporciona
                if (cancion.ImagenPortada != null)
                {
                    var (portadaValida, portadaError) = ValidacionService.ValidarArchivo(
                        cancion.ImagenPortada,
                        ValidacionService.Archivos.ExtensionesImagen,
                        ValidacionService.Archivos.MaxTamanoImagen);
                    if (!portadaValida)
                    {
                        return BadRequest(portadaError);
                    }
                }

                //Subir archivo de audio y obtener la URL
                var audioUrl = await _blobService.SubirArchivoAsync(cancion.ArchivoAudio, "Canciones", "audio");
                var portadaUrl = cancion.ImagenPortada != null
               ? await _blobService.SubirArchivoAsync(cancion.ImagenPortada, "Portadas", "portada")
               : null;

                var duracion = _audioService.ObtenerDuracionAudio(cancion.ArchivoAudio);

                //Creamos la canción en la bdd
                var nuevaCancion = new Cancion
                {
                    Titulo = cancion.Titulo,
                    FechaLanzamiento = cancion.FechaLanzamiento,
                    ArchivoAudio = audioUrl,
                    PortadaUrl = portadaUrl ?? "https://appmelody.blob.core.windows.net/portadas/default.jpg",
                    Duracion = duracion,
                    GeneroId = cancion.GeneroId,
                    AlbumId = cancion.AlbumId,
                    ArtistaId = artista.Id
                };

                _context.Canciones.Add(nuevaCancion);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Canción creada con éxito: {Titulo}", nuevaCancion.Titulo);

                return Ok(new
                {
                    mensaje = "Canción creada con éxito",
                    cancionId = nuevaCancion.Id,
                    titulo = cancion.Titulo,
                    audioUrl = audioUrl,
                    portadaUrl = nuevaCancion.PortadaUrl,
                    artista = artista.NombreArtista

                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la canción");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear la canción");
            }
        }

        [HttpGet("mis-canciones")]
        [Authorize(Roles = "artista")]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerMisCanciones()
        {
            try
            {
                var usuarioActual = await _usuarioService.ObtenerUsuarioActualAsync();
                if (usuarioActual == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }
                var artista = await _context.Artistas.FirstOrDefaultAsync(a => a.UsuarioId == usuarioActual.Id);
                if (artista == null)
                {
                    return BadRequest("El usuario no es un artista válido");
                }
                var canciones = await _context.Canciones
                    .Include(c => c.Artista)
                    .Include(c => c.Genero)
                    .Include(c => c.Album)
                    .Where(c => c.ArtistaId == artista.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Titulo,
                        c.FechaLanzamiento,
                        ArchivoAudioUrl = c.ArchivoAudio,
                        PortadaUrl = c.PortadaUrl,
                        c.Duracion,
                        c.GeneroId,
                        GeneroNombre = c.Genero!.Nombre,
                        c.AlbumId,
                        AlbumNombre = c.Album != null ? c.Album.Titulo : "Sin Álbum",
                        c.ArtistaId,
                        ArtistaNombre = c.Artista!.NombreArtista
                    })
                    .OrderByDescending(c => c.FechaLanzamiento)
                    .ToListAsync();
                return Ok(canciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las canciones del artista");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener las canciones del artista");
            }
        }


        // DELETE: api/Canciones/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "artista,admin")]
        public async Task<IActionResult> DeleteCancion(int id)
        {
            var usuarioActual = await _usuarioService.ObtenerUsuarioActualAsync();
            if (usuarioActual == null)
            {
                return Unauthorized("Usuario no autenticado");
            }
            var cancion = await _context.Canciones.FindAsync(id);
            if (cancion == null)
            {
                return NotFound();
            }
            var esAdmin = User.IsInRole("admin");

            if (!esAdmin)
            {
                var artista = await _usuarioService.ObtenerArtistaActualAsync();
                if (artista?.Id != cancion.ArtistaId)
                    return Forbid("No tienes permisos para eliminar esta canción");
            }
            try
            {
                //Eliminar archivos asociados a la canción
                await _blobService.EliminarArchivoAsync(cancion.ArchivoAudio, "Canciones");
                await _blobService.EliminarArchivoAsync(cancion.PortadaUrl, "Portadas");

                _context.Canciones.Remove(cancion);
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    mensaje = "Canción eliminada con éxito"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar los archivos asociados a la canción");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar los archivos asociados a la canción");

            }
        }

        // GET: api/Canciones/buscar?q=termino - Buscar canciones
        [HttpGet("buscar")]
        public async Task<ActionResult> BuscarCanciones([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var canciones = await _context.Canciones
                    .Include(c => c.Artista)
                    .Where(c => c.Titulo.Contains(q) ||
                               c.Artista!.NombreArtista.Contains(q))
                    .Select(c => new CancionDto
                    {
                        Id = c.Id,
                        Titulo = c.Titulo,
                        ArchivoAudioUrl = c.ArchivoAudio,
                        PortadaUrl = c.PortadaUrl,
                        ArtistaNombre = c.Artista!.NombreArtista
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(canciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar canciones con término: {SearchTerm}", q);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error en la búsqueda");
            }
        }
    }
}