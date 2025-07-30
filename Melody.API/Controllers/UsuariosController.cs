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
    [Authorize]
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

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<UsuarioAdminDto>>> ObtenerUsuarios()
        {
            try
            {
                var usuarios = await _userManager.Users
                    .OrderByDescending(u => u.FechaRegistro)
                    .Take(50)
                    .ToListAsync();

                var usuariosDto = new List<UsuarioAdminDto>();
                foreach (var usuario in usuarios)
                {
                    var roles = await _userManager.GetRolesAsync(usuario);

                    usuariosDto.Add(new UsuarioAdminDto
                    {
                        Id = usuario.Id,
                        Nombre = usuario.Nombre,
                        Apellido = usuario.Apellido,
                        Email = usuario.Email,
                        FotoPerfil = usuario.FotoPerfil,
                        FechaRegistro = usuario.FechaRegistro,
                        EmailConfirmed = usuario.EmailConfirmed,
                        Roles = roles.ToList()
                    });
                }

                return Ok(usuariosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener usuarios");
            }
        }

        // GET: api/Usuarios/5 - Detalle de usuario con suscripciones
        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<UsuarioAdminDto>> ObtenerUsuario(int id)
        {
            try
            {
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
                    .Take(10)
                    .Select(s => new SuscripcionInfoDto
                    {
                        Id = s.Id,
                        FechaInicio = s.FechaInicio,
                        FechaFin = s.FechaFin,
                        EsActiva = s.EsActiva,
                        PlanNombre = s.Plan.Nombre,
                        PlanNumeroUsuarios = s.Plan.NumeroUsuarios,
                        PlanPrecio = s.Plan.Precio
                    })
                    .ToListAsync();

                var usuarioDto = new UsuarioAdminDto
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Apellido = usuario.Apellido,
                    Email = usuario.Email,
                    FotoPerfil = usuario.FotoPerfil,
                    FechaRegistro = usuario.FechaRegistro,
                    EmailConfirmed = usuario.EmailConfirmed,
                    Roles = roles.ToList(),
                    Suscripciones = suscripciones
                };

                return Ok(usuarioDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener usuario");
            }
        }
        // PUT: api/Usuarios/{id}/activar-email - Admin activa email de usuario
        [HttpPut("{id}/activar-email")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> ActivarEmail(int id)
        {
            try
            {
                var usuario = await _userManager.FindByIdAsync(id.ToString());
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                if (usuario.EmailConfirmed)
                {
                    return BadRequest("El email ya está confirmado");
                }

                usuario.EmailConfirmed = true;
                var resultado = await _userManager.UpdateAsync(usuario);

                if (resultado.Succeeded)
                {
                    _logger.LogInformation("Admin activó email para usuario {Email}", usuario.Email);
                    return Ok(new { mensaje = "Email activado exitosamente" });
                }

                return BadRequest("Error al activar email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar email del usuario {Id}", id);
                return StatusCode(500, "Error interno");
            }
        }
        // GET: api/Usuarios/buscar?q=nombre - Buscar usuarios con detección automática de roles
        [HttpGet("buscar")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<UsuarioAdminDto>>> BuscarUsuarios([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var terminoBusqueda = q.ToLower().Trim();

                // ✅ DETECTAR SI EL TÉRMINO ES UN ROL
                var esRol = false;
                var rolBuscado = "";

                switch (terminoBusqueda)
                {
                    case "admin":
                    case "administrador":
                        esRol = true;
                        rolBuscado = "admin";
                        break;
                    case "premium":
                    case "userpremium":
                        esRol = true;
                        rolBuscado = "userpremium";
                        break;
                    case "free":
                    case "userfree":
                    case "gratuito":
                    case "gratis":
                        esRol = true;
                        rolBuscado = "userfree";
                        break;
                    case "artista":
                    case "artist":
                    case "músico":
                        esRol = true;
                        rolBuscado = "artista";
                        break;
                }

                List<Usuario> usuarios;

                // ✅ SI ES BÚSQUEDA POR ROL, TRAER TODOS LOS USUARIOS
                if (esRol)
                {
                    usuarios = await _userManager.Users
                        .OrderByDescending(u => u.FechaRegistro)
                        .Take(200) // Más usuarios para filtrar por rol
                        .ToListAsync();
                }
                else
                {
                    // ✅ SI NO ES ROL, BUSCAR POR TEXTO NORMAL
                    usuarios = await _userManager.Users
                        .Where(u => u.Nombre.Contains(q) ||
                                   u.Apellido.Contains(q) ||
                                   u.Email.Contains(q))
                        .OrderByDescending(u => u.FechaRegistro)
                        .Take(50)
                        .ToListAsync();
                }

                var usuariosDto = new List<UsuarioAdminDto>();

                foreach (var usuario in usuarios)
                {
                    var roles = await _userManager.GetRolesAsync(usuario);

                    // ✅ SI ES BÚSQUEDA POR ROL, SOLO INCLUIR USUARIOS CON ESE ROL
                    if (esRol)
                    {
                        if (!roles.Any(r => r.ToLower() == rolBuscado))
                        {
                            continue; // Saltar usuarios que no tienen el rol
                        }
                    }

                    usuariosDto.Add(new UsuarioAdminDto
                    {
                        Id = usuario.Id,
                        Nombre = usuario.Nombre,
                        Apellido = usuario.Apellido,
                        Email = usuario.Email,
                        FotoPerfil = usuario.FotoPerfil,
                        FechaRegistro = usuario.FechaRegistro,
                        EmailConfirmed = usuario.EmailConfirmed,
                        Roles = roles.ToList()
                    });
                }

                // Limitar resultados finales
                var resultadosFinales = usuariosDto.Take(50).ToList();

                return Ok(resultadosFinales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar usuarios con término: {SearchTerm}", q);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error en la búsqueda");
            }
        }


        // DELETE: api/Usuarios/{id} - Admin elimina usuario
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> EliminarUsuario(int id)
        {
            try
            {
                var usuarioActual = await _usuarioService.ObtenerUsuarioActualAsync();

                // No puede eliminarse a sí mismo
                if (usuarioActual.Id == id)
                {
                    return BadRequest("No puedes eliminar tu propia cuenta");
                }

                var usuario = await _userManager.FindByIdAsync(id.ToString());
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                // Verificar si tiene suscripciones activas
                var tieneSubActiva = await _context.Suscripciones
                    .AnyAsync(s => s.UsuarioId == id && s.EsActiva);

                if (tieneSubActiva)
                {
                    return BadRequest("No se puede eliminar un usuario con suscripción activa");
                }

                var resultado = await _userManager.DeleteAsync(usuario);
                if (resultado.Succeeded)
                {
                    _logger.LogInformation("Admin eliminó usuario {Email}", usuario.Email);
                    return Ok(new { mensaje = "Usuario eliminado exitosamente" });
                }

                return BadRequest("Error al eliminar usuario");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {Id}", id);
                return StatusCode(500, "Error interno");
            }
        }

        //GET: api/Usuarios/mi-perfil - Obtiene el perfil del usuario autenticado

        [HttpGet("mi-perfil")]
        public async Task<ActionResult<MiPerfilDto>> ObtenerMiPerfil()
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();
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
    }
}
