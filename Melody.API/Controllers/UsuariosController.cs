using Melody.Modelos.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Melody.API.Services;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<UsuariosController> _logger;
        private readonly IAzureBlobService _blobService;
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(AppDbContext context, UserManager<Usuario> userManger, IAzureBlobService blobService,
                                    IUsuarioService usuarioService, ILogger<UsuariosController> logger)
        {
            _context = context;
            _userManager = userManger;
            _blobService = blobService;
            _usuarioService = usuarioService;
            _logger = logger;
        }

        // GET: api/Usuarios
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<Usuario>>> ObtenerUsuarios()
        {
            try
            {
                var usuarios = await _userManager.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Nombre,
                        u.Apellido,
                        u.Email,
                        u.FotoPerfil,
                        u.FechaRegistro,
                        u.EmailConfirmed
                    })
                    .OrderByDescending(u => u.FechaRegistro)
                    .ToListAsync();
                //Agregar roles a cada usuario
                var usuariosConRoles = new List<object>();
                foreach (var usuario in usuarios)
                {
                    var usuarioEntity = await _userManager.FindByIdAsync(usuario.Id.ToString());
                    var roles = await _userManager.GetRolesAsync(usuarioEntity);

                    usuariosConRoles.Add(new
                    {
                        usuario.Id,
                        usuario.Nombre,
                        usuario.Apellido,
                        usuario.Email,
                        usuario.FotoPerfil,
                        usuario.FechaRegistro,
                        usuario.EmailConfirmed,
                        Roles = roles
                    });
                }
                return Ok(usuariosConRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener usuarios");
            }
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Usuario>> ObtenerUsuario(int id)
        {
            try
            {
                var usuarioActual = await _usuarioService.ObtenerUsuarioActualAsync();
                if (usuarioActual == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                // Verificar si es admin o el mismo usuario
                var rolesActual = await _userManager.GetRolesAsync(usuarioActual);
                if (!rolesActual.Contains("admin") && usuarioActual.Id != id)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "No tienes permisos para ver este usuario");
                }

                var usuario = await _userManager.FindByIdAsync(id.ToString());
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado");
                }
                var roles = await _userManager.GetRolesAsync(usuario);
                var suscripciones = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Where(s => s.UsuarioId == id)
                    .OrderByDescending(s => s.FechaInicio)
                    .Take(5)
                    .Select(s => new
                    {
                        s.Id,
                        s.PlanId,
                        PlanNombre = s.Plan.Nombre,
                        s.FechaInicio,
                        s.FechaFin
                    })
                    .ToListAsync();
                var resultado = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Apellido,
                    usuario.Email,
                    usuario.FotoPerfil,
                    usuario.FechaRegistro,
                    usuario.EmailConfirmed,
                    Roles = roles,
                    Suscripciones = suscripciones
                };
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener usuario");
            }
        }

        //GET: api/Usuarios/mi-perfil - Obtiene el perfil del usuario autenticado

        [HttpGet("mi-perfil")]
        [Authorize]
        public async Task<ActionResult<MiPerfilDto>> ObtenerMiPerfil()
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var roles = await _userManager.GetRolesAsync(usuario);
                var suscripcionActiva = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Where(s => s.UsuarioId == usuario.Id && s.EsActiva)
                    .FirstOrDefaultAsync();

                var perfil = new MiPerfilDto
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Apellido = usuario.Apellido,
                    Email = usuario.Email,
                    FotoPerfil = usuario.FotoPerfil,
                    EmailConfirmed = usuario.EmailConfirmed,
                    Roles = roles.ToList(),
                    TieneSuscripcionActiva = suscripcionActiva != null,
                    PlanNombre = suscripcionActiva?.Plan?.Nombre,
                    PlanPrecio = suscripcionActiva?.Plan?.Precio,
                    SuscripcionFechaInicio = suscripcionActiva?.FechaInicio,
                    SuscripcionFechaFin = suscripcionActiva?.FechaFin
                };

                return Ok(perfil);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el perfil del usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el perfil");
            }
        }

        // PUT: api/Usuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("mi-perfil")]
        public async Task<IActionResult> ActualizarPerfil([FromForm] ActualizarPerfilUsuarioDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var usuarioActual = await _usuarioService.ObtenerUsuarioActualAsync();
                if (usuarioActual == null)
                {
                    return Unauthorized();
                }
                //Acutalizar foto de perfil si se proporciona
                if (dto.FotoPerfil != null)
                {
                    var (imagenValida, imagenError) = ValidacionService.ValidarArchivo(dto.FotoPerfil,
                            ValidacionService.Archivos.ExtensionesImagen,
                            ValidacionService.Archivos.MaxTamanoImagen);

                    if (!imagenValida)
                        return BadRequest(imagenError);

                    // Eliminar foto anterior
                    await _blobService.EliminarArchivoAsync(usuarioActual.FotoPerfil, "Perfiles");

                    // Subir nueva foto
                    usuarioActual.FotoPerfil = await _blobService.SubirArchivoAsync(
                        dto.FotoPerfil, "Perfiles", "usuario");
                }
                // Actualizar otros campos
                if (!string.IsNullOrEmpty(dto.Nombre))
                    usuarioActual.Nombre = dto.Nombre;
                if (!string.IsNullOrEmpty(dto.Apellido))
                    usuarioActual.Apellido = dto.Apellido;
                var resultado = await _userManager.UpdateAsync(usuarioActual);
                if (!resultado.Succeeded)
                {
                    return BadRequest(resultado.Errors);
                }

                return Ok(new
                {
                    mensaje = "Perfil actualizado con éxito",
                    usuario = new
                    {
                        usuarioActual.Id,
                        usuarioActual.Nombre,
                        usuarioActual.Apellido,
                        usuarioActual.Email,
                        usuarioActual.FotoPerfil
                    }
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil de usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar perfil");
            }
        }

        // GET: api/Usuarios/buscar?q=nombre - Buscar usuarios (solo admin)
        [HttpGet("buscar")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarUsuarios([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var usuarios = await _userManager.Users
                    .Where(u => u.Nombre.Contains(q) ||
                               u.Apellido.Contains(q) ||
                               u.Email.Contains(q))
                    .Select(u => new
                    {
                        u.Id,
                        u.Nombre,
                        u.Apellido,
                        u.Email,
                        u.FotoPerfil
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar usuarios con término: {SearchTerm}", q);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error en la búsqueda");
            }
        }
    }
}