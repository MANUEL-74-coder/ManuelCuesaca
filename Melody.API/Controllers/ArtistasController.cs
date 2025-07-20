using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Identity;
using Azure.Storage.Blobs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Melody.Modelos.DTOs;
using Melody.API.Services;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArtistasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAzureBlobService _blobService;
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<ArtistasController> _logger;


        public ArtistasController(AppDbContext context, IAzureBlobService blobService,
                                    IUsuarioService usuarioService, ILogger<ArtistasController> logger)
        {
            _context = context;
            _blobService = blobService;
            _usuarioService = usuarioService;
            _logger = logger;
        }

        //Obtener todos los artistas públicos
        // GET: api/Artistas 
        [HttpGet]
        public async Task<IActionResult> ObtenerArtistas()
        {
            try
            {
                var artistas = await _context.Artistas.ToListAsync();
                return Ok(artistas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener artistas");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener artistas");
            }
        }

        // GET: api/Artistas/5 - Obtener artista específico (público)
        // GET: api/Artistas/5 - Obtener artista específico (público)
        [HttpGet("{id}")]
        public async Task<ActionResult<ArtistaDto>> ObtenerArtista(int id)
        {
            try
            {
                // Primero obtenemos los datos básicos del artista
                var artista = await _context.Artistas
                    .Include(a => a.Usuario)
                    .Include(a => a.Seguidores)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (artista == null)
                {
                    return NotFound("Artista no encontrado");
                }

                // Luego obtenemos las canciones por separado
                var canciones = await _context.Canciones
                    .Include(c => c.Genero)
                    .Where(c => c.ArtistaId == id)
                    .Select(c => new CancionDto
                    {
                        Id = c.Id,
                        Titulo = c.Titulo,
                        FechaLanzamiento = c.FechaLanzamiento,
                        PortadaUrl = c.PortadaUrl,
                        Duracion = c.Duracion.HasValue ? c.Duracion.Value : TimeSpan.Zero,
                        GeneroId = c.GeneroId,
                        GeneroNombre = c.Genero!.Nombre,
                        AlbumId = c.AlbumId,
                        AlbumNombre = c.Album != null ? c.Album.Titulo : null,
                        ArtistaId = c.ArtistaId,
                        ArtistaNombre = artista.NombreArtista,
                        ArchivoAudioUrl = c.ArchivoAudio ?? string.Empty
                    })
                    .OrderByDescending(c => c.FechaLanzamiento)
                    .Take(10)
                    .ToListAsync();

                // Y los álbums por separado (para el conteo)
                var totalAlbums = await _context.Albums
                    .CountAsync(a => a.ArtistaId == id);

                // Construimos el ArtistaDto
                var artistaDto = new ArtistaDto
                {
                    Id = artista.Id,
                    NombreArtista = artista.NombreArtista,
                    Biografia = artista.Biografia,
                    ImagenPerfil = artista.ImagenPerfil,
                    FechaRegistro = artista.Usuario!.FechaRegistro,
                    TotalSeguidores = artista.Seguidores?.Count ?? 0,
                    TotalCanciones = await _context.Canciones.CountAsync(c => c.ArtistaId == id),
                    TotalAlbums = totalAlbums,
                    Canciones = canciones
                };

                return Ok(artistaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el artista con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el artista");
            }
        }
        // GET: api/Artistas/5/detalle
        [HttpGet("detalle/{id}")]
        public async Task<ActionResult<ArtistaDto>> ObtenerArtistaDetalle(int id)
        {
            try
            {
                var artista = await _context.Artistas
                    .Include(a => a.Usuario)
                    .Where(a => a.Id == id)
                    .FirstOrDefaultAsync();

                if (artista == null)
                {
                    return NotFound("Artista no encontrado");
                }

                // Obtener canciones del artista
                var canciones = await _context.Canciones
                    .Include(c => c.Genero)
                    .Include(c => c.Album)
                    .Where(c => c.ArtistaId == id)
                    .Select(c => new CancionDto
                    {
                        Id = c.Id,
                        Titulo = c.Titulo,
                        FechaLanzamiento = c.FechaLanzamiento,
                        ArchivoAudioUrl = c.ArchivoAudio,
                        PortadaUrl = c.PortadaUrl,
                        Duracion = c.Duracion,
                        GeneroId = c.GeneroId,
                        GeneroNombre = c.Genero!.Nombre,
                        AlbumId = c.AlbumId,
                        AlbumNombre = c.Album != null ? c.Album.Titulo : "Sin Álbum",
                        ArtistaId = c.ArtistaId,
                        ArtistaNombre = artista.NombreArtista

                    })
                    .OrderByDescending(c => c.FechaLanzamiento)
                    .ToListAsync();

                // Contar álbumes únicos
                var totalAlbums = await _context.Canciones
                    .Where(c => c.ArtistaId == id && c.AlbumId != null)
                    .Select(c => c.AlbumId)
                    .Distinct()
                    .CountAsync();

                // Contar seguidores (si tienes tabla de seguimientos)
                var totalSeguidores = await _context.Seguimientos
                    .CountAsync(s => s.ArtistaId == id);

                var artistaDto = new ArtistaDto
                {
                    Id = artista.Id,
                    NombreArtista = artista.NombreArtista,
                    Biografia = artista.Biografia,
                    ImagenPerfil = artista.ImagenPerfil,
                    TotalCanciones = canciones.Count,
                    TotalAlbums = totalAlbums,
                    TotalSeguidores = totalSeguidores,
                    Canciones = canciones
                };

                return Ok(artistaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del artista con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener detalles del artista");
            }
        }



        // GET: api/Artistas/mi-perfil - Obtener perfil del artista autenticado
        [HttpGet("mi-perfil")]
        [Authorize(Roles = "artista")]
        public async Task<ActionResult> ObtenerMiPerfil()
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");

                }
                var artista = await _context.Artistas
                    .Include(a => a.Usuario)
                    .Include(a => a.Canciones)
                    .Include(a => a.Albums)
                    .Include(a => a.Seguidores)
                    .FirstOrDefaultAsync(a => a.UsuarioId == usuario.Id);
                if (artista == null)
                {
                    return NotFound("Perfil de artista no encontrado");
                }
                var perfil = new
                {
                    artista.Id,
                    artista.NombreArtista,
                    artista.Biografia,
                    artista.ImagenPerfil,
                    Usuario = new
                    {
                        usuario.Nombre,
                        usuario.Apellido,
                        usuario.Email,
                        usuario.FotoPerfil,
                        usuario.FechaRegistro
                    },
                    Estadisticas = new
                    {
                        TotalCanciones = artista.Canciones?.Count ?? 0,
                        TotalAlbums = artista.Albums?.Count ?? 0,
                        TotalSeguidores = artista.Seguidores?.Count ?? 0
                    }
                };
                return Ok(perfil);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el perfil del artista");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el perfil");
            }
        }
        // PUT: api/Artistas/mi-perfil
        [HttpPut("mi-perfil")]
        [Authorize(Roles = "artista")]
        public async Task<IActionResult> ActualizarPerfil([FromForm] ActualizarPerfilArtistaDto dto)
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

                var artista = await _context.Artistas
                    .FirstOrDefaultAsync(a => a.UsuarioId == usuario.Id);

                if (artista == null)
                {
                    return NotFound("Perfil de artista no encontrado");
                }

                // Actualizar imagen de perfil del artista si se proporciona
                if (dto.ImagenPerfil != null)
                {
                    var (imagenValida, imagenError) = ValidacionService.ValidarArchivo(
                    dto.ImagenPerfil,
                    ValidacionService.Archivos.ExtensionesImagen,
                    ValidacionService.Archivos.MaxTamanoImagen);

                    if (!imagenValida)
                        return BadRequest(imagenError);

                    // Eliminar imagen anterior si existe
                    await _blobService.EliminarArchivoAsync(artista.ImagenPerfil, "Perfiles");

                    // Subir nueva imagen
                    artista.ImagenPerfil = await _blobService.SubirArchivoAsync(dto.ImagenPerfil, "Perfiles", "perfil");
                }

                // Actualizar solo los campos del artista
                if (!string.IsNullOrEmpty(dto.NombreArtista))
                    artista.NombreArtista = dto.NombreArtista;

                artista.Biografia = dto.Biografia; // Puede ser null

                await _context.SaveChangesAsync();

                _logger.LogInformation("Perfil de artista actualizado: {NombreArtista}", artista.NombreArtista);

                return Ok(new
                {
                    mensaje = "Perfil de artista actualizado con éxito",
                    artista = new
                    {
                        artista.Id,
                        artista.NombreArtista,
                        artista.Biografia,
                        artista.ImagenPerfil
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el perfil del artista");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el perfil del artista");
            }
        }

        // GET: api/Artistas/buscar?q=nombre - Buscar artistas
        [HttpGet("buscar")]
        public async Task<ActionResult> BuscarArtistas([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var artistas = await _context.Artistas
                    .Include(a => a.Usuario)
                    .Where(a => a.NombreArtista.Contains(q) ||
                               (a.Usuario!.Nombre + " " + a.Usuario.Apellido).Contains(q))
                    .Select(a => new
                    {
                        a.Id,
                        a.NombreArtista,
                        a.ImagenPerfil,
                        NombreCompleto = a.Usuario!.Nombre + " " + a.Usuario.Apellido
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(artistas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar artistas con término: {SearchTerm}", q);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error en la búsqueda");
            }
        }
    }
}