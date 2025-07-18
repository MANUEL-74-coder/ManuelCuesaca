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
using Melody.Modelos.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using NAudio;
using Microsoft.IdentityModel.Tokens;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CancionesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobContainerClient _cancionesContainer;
        private readonly BlobContainerClient _portadasContainer;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<CancionesController> _logger;

        public CancionesController(AppDbContext context, ILogger<CancionesController> logger, IConfiguration configuration, UserManager<Usuario> userManager)
        {
            _context = context;
            _logger = logger;

            string cancionesSasUrl = configuration["AzureStorage:Canciones"];
            string portadasSasUrl = configuration["AzureStorage:Portadas"];
            _portadasContainer = new BlobContainerClient(new Uri(portadasSasUrl));
            _cancionesContainer = new BlobContainerClient(new Uri(cancionesSasUrl));
            _userManager = userManager;
        }

        //Helper
        private async Task<Usuario?> ObtenerUsuarioActualAsyn()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _userManager.FindByIdAsync(userId.ToString());
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
                var usuarioActual = await ObtenerUsuarioActualAsyn();
                if (usuarioActual == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }
                var artista = await _context.Artistas.FirstOrDefaultAsync(a => a.UsuarioId == usuarioActual.Id);
                if (artista == null)
                {
                    return BadRequest("El usuario no es un artista válido");
                }
                var cancionExistente = await _context.Canciones.FindAsync(id);
                if (cancionExistente == null)
                {
                    return NotFound("Canción no encontrada");
                }
                if (cancionExistente.ArtistaId != artista.Id)
                {
                    return Forbid("No tienes permiso para actualizar esta canción");
                }
                //Actualizar archivo de audio si se proporciona uno nuevo
                if (cancion.ArchivoAudio != null)
                {
                    var extensionesAudioPermitidas = new[] { ".mp3", ".wav", ".flac" };
                    var extensionAudio = Path.GetExtension(cancion.ArchivoAudio.FileName).ToLower();
                    if (!extensionesAudioPermitidas.Contains(extensionAudio))
                    {
                        return BadRequest("Formato de archivo de audio no permitido. Use .mp3, .wav o .flac");
                    }
                    if (cancion.ArchivoAudio.Length > 50 * 1024 * 1024)
                    {
                        return BadRequest("El archivo de audio no puede exceder los 50 MB");
                    }
                    //Eliminar archivo de audio anterior
                    await EliminarArchivoBlobAsync(cancionExistente.ArchivoAudio, _cancionesContainer);

                    //Subir nuevo archivo de audio
                    cancionExistente.ArchivoAudio = await SubirArchivoAudio(cancion.ArchivoAudio);

                    cancionExistente.Duracion = ObtenerDuracionAudio(cancion.ArchivoAudio);
                }
                //Actualizar imagen de portada si se proporciona una nueva
                if (cancion.ImagenPortada != null)
                {
                    var extensionesPortadaPermitidas = new[] { ".jpg", ".jpeg", ".png" };
                    var extensionPortada = Path.GetExtension(cancion.ImagenPortada.FileName).ToLower();
                    if (!extensionesPortadaPermitidas.Contains(extensionPortada))
                    {
                        return BadRequest("Formato de imagen de portada no permitido. Use .jpg, .jpeg o .png");
                    }
                    if (cancion.ImagenPortada.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest("La imagen de portada no puede exceder los 5 MB");
                    }
                    //Eliminar imagen de portada anterior
                    await EliminarArchivoBlobAsync(cancionExistente.PortadaUrl, _portadasContainer);

                    //Subir nueva imagen de portada
                    cancionExistente.PortadaUrl = await SubirImagenPortada(cancion.ImagenPortada);
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
                    titulo = cancionExistente.Titulo,
                    audioUrl = cancionExistente.ArchivoAudio,
                    portadaUrl = cancionExistente.PortadaUrl,
                    artista = artista.NombreArtista
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
                var usuarioActual = await ObtenerUsuarioActualAsyn();
                if (usuarioActual == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }
                var artista = await _context.Artistas.FirstOrDefaultAsync(a => a.UsuarioId == usuarioActual.Id);
                if (artista == null)
                {
                    return BadRequest("El usuario no es un artista válido");
                }
                if (cancion.ArchivoAudio == null || cancion.ArchivoAudio.Length == 0)
                {
                    return BadRequest("Debe seleccionar un archivo de audio");
                }
                var extensionesAudioPermitidas = new[] { ".mp3", ".wav", ".flac" };
                var extensionAudio = Path.GetExtension(cancion.ArchivoAudio.FileName).ToLower();

                if (!extensionesAudioPermitidas.Contains(extensionAudio))
                {
                    return BadRequest("Formato de archivo de audio no permitido. Use .mp3, .wav o .flac");
                }
                if (cancion.ArchivoAudio.Length > 50 * 1024 * 1024)
                {
                    return BadRequest("El archivo de audio no puede exceder los 50 MB");
                }
                if (cancion.ImagenPortada != null)
                {
                    var extensionesPortadaPermitidas = new[] { ".jpg", ".jpeg", ".png" };
                    var extensionPortada = Path.GetExtension(cancion.ImagenPortada.FileName).ToLower();

                    if (!extensionesPortadaPermitidas.Contains(extensionPortada))
                    {
                        return BadRequest("Formato de imagen de portada no permitido. Use .jpg, .jpeg o .png");
                    }
                    if (cancion.ImagenPortada.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest("La imagen de portada no puede exceder los 5 MB");
                    }
                }
                //Subir archivo de audio
                string audioUrl;
                try
                {
                    audioUrl = await SubirArchivoAudio(cancion.ArchivoAudio);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al subir el archivo de audio");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error al subir el archivo de audio");
                }

                //Subir imagen de portada si existe
                string? portadaUrl = null;
                if (cancion.ImagenPortada != null)
                {
                    try
                    {
                        portadaUrl = await SubirImagenPortada(cancion.ImagenPortada);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al subir la imagen de portada");
                        return StatusCode(StatusCodes.Status500InternalServerError, "Error al subir la imagen de portada");
                    }
                }

                var duracion = ObtenerDuracionAudio(cancion.ArchivoAudio);

                //Creamos la canción en la bdd
                var nuevaCancion = new Cancion
                {
                    Titulo = cancion.Titulo,
                    FechaLanzamiento = cancion.FechaLanzamiento,
                    ArchivoAudio = audioUrl,
                    PortadaUrl = portadaUrl,
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

                    titulo = cancion.Titulo,
                    audioUrl = cancion.ArchivoAudio,
                    portadaUrl = cancion.ImagenPortada,
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
                var usuarioActual = await ObtenerUsuarioActualAsyn();
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
            var usuarioActual = await ObtenerUsuarioActualAsyn();
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
                var artista = await _context.Artistas.FirstOrDefaultAsync(a => a.UsuarioId == usuarioActual.Id);
                if (artista == null)
                {
                    return BadRequest("El usuario no es un artista válido");
                }
                if (cancion.ArtistaId != artista.Id)
                {
                    return Forbid("No tienes permiso para eliminar esta canción");
                }
            }
            try
            {
                //Eliminar archivos asociados a la canción
                await EliminarArchivoBlobAsync(cancion.ArchivoAudio, _cancionesContainer);
                await EliminarArchivoBlobAsync(cancion.PortadaUrl, _portadasContainer);

                _context.Canciones.Remove(cancion);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Canción eliminada con éxito: {Titulo}", cancion.Titulo);
                return Ok(new
                {
                    mensaje = "Canción eliminada con éxito",
                    cancionId = cancion.Id,
                    titulo = cancion.Titulo
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
        private async Task<string> SubirArchivoAudio(IFormFile archivoAudio)
        {
            var extension = Path.GetExtension(archivoAudio.FileName).ToLower();
            var nombreAudio = $"audio_{Guid.NewGuid()}{extension}";
            var blobAudio = _cancionesContainer.GetBlobClient(nombreAudio);

            using (var stream = archivoAudio.OpenReadStream())
            {
                await blobAudio.UploadAsync(stream, true);
            }

            return $"https://appmelody.blob.core.windows.net/canciones/{nombreAudio}";
        }

        private async Task<string> SubirImagenPortada(IFormFile imagenPortada)
        {
            var extension = Path.GetExtension(imagenPortada.FileName).ToLower();
            var nombrePortada = $"portada_{Guid.NewGuid()}{extension}";
            var blobPortada = _portadasContainer.GetBlobClient(nombrePortada);

            using (var stream = imagenPortada.OpenReadStream())
            {
                await blobPortada.UploadAsync(stream, true);
            }

            return $"https://appmelody.blob.core.windows.net/portadas/{nombrePortada}";
        }

        private async Task EliminarArchivoBlobAsync(string url, BlobContainerClient container)
        {
            if (!string.IsNullOrEmpty(url))
            {
                var uri = new Uri(url);
                var blobName = uri.Segments.Last();
                var blobClient = container.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
            }
        }

        private TimeSpan? ObtenerDuracionAudio(IFormFile archivoAudio)
        {
            try
            {
                using (var stream = archivoAudio.OpenReadStream())
                using (var reader = new NAudio.Wave.Mp3FileReader(stream))
                {
                    return reader.TotalTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la duración del archivo {FileName} (Tamaño: {Size} bytes, Tipo: {ContentType})",
                    archivoAudio.FileName,
                    archivoAudio.Length,
                    archivoAudio.ContentType);
                return null;
            }
        }


        private bool CancionExists(int id)
        {
            return _context.Canciones.Any(e => e.Id == id);
        }
    }
}