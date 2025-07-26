using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Melody.Modelos.PayPal;
using Melody.API.Services;
using Melody.Modelos.DTOs;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuscripcionesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<SuscripcionesController> _logger;

        public SuscripcionesController(AppDbContext context, UserManager<Usuario> userManager,
                                      IUsuarioService usuarioService, ILogger<SuscripcionesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _usuarioService = usuarioService;
            _logger = logger;
        }

        // GET: api/Suscripciones/mi-suscripcion - Obtener mi suscripción activa
        [HttpGet("mi-suscripcion")]
        [Authorize]
        public async Task<ActionResult<MiSuscripcionResponseDto>> ObtenerMiSuscripcion()
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                // Buscar como propietario
                var suscripcionPropietario = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Include(s => s.Pagos)
                    .Include(s => s.Miembros.Where(m => m.EsActivo))
                        .ThenInclude(m => m.Usuario)
                    .Where(s => s.UsuarioId == usuario.Id && s.EsActiva)
                    .FirstOrDefaultAsync();

                // Buscar como miembro familiar
                var suscripcionMiembro = await _context.SuscripcionMiembros
                    .Include(m => m.Suscripcion).ThenInclude(s => s.Plan)
                    .Include(m => m.Suscripcion.Usuario)
                    .Include(m => m.Suscripcion.Miembros.Where(mb => mb.EsActivo))
                        .ThenInclude(mb => mb.Usuario)
                    .Where(m => m.UsuarioId == usuario.Id && m.EsActivo && m.Suscripcion.EsActiva)
                    .FirstOrDefaultAsync();

                if (suscripcionPropietario == null && suscripcionMiembro == null)
                {
                    return Ok(new { mensaje = "No tienes una suscripción activa", tienesSuscripcion = false });
                }

                var suscripcionActiva = suscripcionPropietario ?? suscripcionMiembro?.Suscripcion;
                var esPropietario = suscripcionPropietario != null;
                var miembrosCount = suscripcionActiva.Miembros?.Count() ?? 0;

                var response = new MiSuscripcionResponseDto
                {
                    TienesSuscripcion = true,
                    EsPropietario = esPropietario,
                    Suscripcion = new SuscripcionInfoDto
                    {
                        Id = suscripcionActiva.Id,
                        FechaInicio = suscripcionActiva.FechaInicio,
                        FechaFin = suscripcionActiva.FechaFin,
                        EsActiva = suscripcionActiva.EsActiva,
                        PlanNombre = suscripcionActiva.Plan.Nombre,
                        PlanNumeroUsuarios = suscripcionActiva.Plan.NumeroUsuarios,
                        PlanPrecio = suscripcionActiva.Plan.Precio,
                        DiasRestantes = Math.Max(0, (suscripcionActiva.FechaFin - DateTime.Now).Days),
                        MiembrosActivos = esPropietario ? miembrosCount : null,
                        TotalUsuarios = esPropietario ? miembrosCount + 1 : null,
                        EspaciosDisponibles = esPropietario ? suscripcionActiva.Plan.NumeroUsuarios - (miembrosCount + 1) : null
                    }
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener suscripción");
                return StatusCode(500, "Error interno");
            }
        }

        // POST: api/Suscripciones/{id}/agregar-miembro
        [HttpPost("{id}/agregar-miembro")]
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult> AgregarMiembroFamiliar(int id, [FromBody] AgregarMiembroRequest request)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                var suscripcion = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Include(s => s.Miembros.Where(m => m.EsActivo))
                    .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuario.Id && s.EsActiva);

                if (suscripcion == null)
                    return NotFound("Suscripción no encontrada");

                if (suscripcion.Plan.NumeroUsuarios <= 1)
                    return BadRequest("Tu plan no permite miembros familiares");

                var miembrosActuales = suscripcion.Miembros?.Count() ?? 0;
                if (miembrosActuales + 1 >= suscripcion.Plan.NumeroUsuarios)
                    return BadRequest($"Límite de {suscripcion.Plan.NumeroUsuarios} usuarios alcanzado");

                var usuarioAAgregar = await _userManager.FindByEmailAsync(request.Email);
                if (usuarioAAgregar == null)
                    return NotFound("Usuario no encontrado");

                if (usuarioAAgregar.Id == usuario.Id)
                    return BadRequest("No puedes agregarte a ti mismo");

                // Verificar que no tenga suscripción activa
                var tieneSubActiva = await _context.Suscripciones
                    .AnyAsync(s => s.UsuarioId == usuarioAAgregar.Id && s.EsActiva);
                var esMiembroActivo = await _context.SuscripcionMiembros
                    .AnyAsync(m => m.UsuarioId == usuarioAAgregar.Id && m.EsActivo);

                if (tieneSubActiva || esMiembroActivo)
                    return BadRequest("El usuario ya tiene acceso premium activo");

                // Crear miembro
                var nuevoMiembro = new SuscripcionMiembro
                {
                    SuscripcionId = id,
                    UsuarioId = usuarioAAgregar.Id,
                    FechaUnion = DateTime.Now,
                    EsActivo = true,
                    Rol = "Miembro",
                    Usuario = usuarioAAgregar
                };

                _context.SuscripcionMiembros.Add(nuevoMiembro);
                await _context.SaveChangesAsync();

                // Cambiar rol a premium
                var roles = await _userManager.GetRolesAsync(usuarioAAgregar);
                if (roles.Contains("userfree"))
                {
                    await _userManager.RemoveFromRoleAsync(usuarioAAgregar, "userfree");
                    await _userManager.AddToRoleAsync(usuarioAAgregar, "userpremium");
                }

                return Ok("Miembro agregado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar miembro");
                return StatusCode(500, "Error interno");
            }
        }

        // GET: api/Suscripciones/{id}/miembros
        [HttpGet("{id}/miembros")]
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult<MiembrosFamiliaResponseDto>> ObtenerMiembrosFamiliares(int id)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                var suscripcion = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Include(s => s.Usuario)
                    .Include(s => s.Miembros.Where(m => m.EsActivo))
                        .ThenInclude(m => m.Usuario)
                    .FirstOrDefaultAsync(s => s.Id == id && s.EsActiva);

                if (suscripcion == null)
                    return NotFound("Suscripción no encontrada");

                // Verificar permisos
                bool esOwner = suscripcion.UsuarioId == usuario.Id;
                bool esMiembro = suscripcion.Miembros?.Any(m => m.UsuarioId == usuario.Id) ?? false;

                if (!esOwner && !esMiembro)
                    return Forbid();

                var miembros = suscripcion.Miembros?.Select(m => new
                {
                    m.Id,
                    m.FechaUnion,
                    Usuario = new
                    {
                        Id = m.Usuario.Id,
                        m.Usuario.Email,
                        m.Usuario.Nombre,
                        m.Usuario.Apellido
                    }
                }).ToList();

                var response = new MiembrosFamiliaResponseDto
                {
                    UsuarioPrincipalEmail = suscripcion.Usuario.Email,
                    UsuarioPrincipalNombre = $"{suscripcion.Usuario.Nombre} {suscripcion.Usuario.Apellido}",
                    Miembros = miembros?.Select(m => new MiembroFamiliarDto
                    {
                        Id = m.Id,
                        UsuarioId = m.Usuario.Id,
                        Email = m.Usuario.Email,
                        Nombre = m.Usuario.Nombre,
                        Apellido = m.Usuario.Apellido,
                        FechaUnion = m.FechaUnion
                    }).ToList() ?? new(),
                    TotalUsuarios = (miembros?.Count ?? 0) + 1,
                    EspaciosDisponibles = suscripcion.Plan.NumeroUsuarios - ((miembros?.Count ?? 0) + 1),
                    PuedeAgregarMas = esOwner && suscripcion.Plan.NumeroUsuarios > ((miembros?.Count ?? 0) + 1)
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener miembros");
                return StatusCode(500, "Error interno");
            }
        }

        // DELETE: api/Suscripciones/{id}/miembros/{usuarioId}
        [HttpDelete("{id}/miembros/{usuarioId}")]
        [Authorize(Roles = "userpremium")]
        public async Task<ActionResult> RemoverMiembroFamiliar(int id, int usuarioId)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                var suscripcion = await _context.Suscripciones
                    .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuario.Id && s.EsActiva);

                if (suscripcion == null)
                    return NotFound("Suscripción no encontrada");

                var miembro = await _context.SuscripcionMiembros
                    .Include(m => m.Usuario)
                    .FirstOrDefaultAsync(m => m.SuscripcionId == id && m.UsuarioId == usuarioId && m.EsActivo);

                if (miembro == null)
                    return NotFound("Miembro no encontrado");

                // Desactivar miembro
                miembro.EsActivo = false;
                await _context.SaveChangesAsync();

                // Cambiar rol a free
                var roles = await _userManager.GetRolesAsync(miembro.Usuario);
                if (roles.Contains("userpremium"))
                {
                    await _userManager.RemoveFromRoleAsync(miembro.Usuario, "userpremium");
                    await _userManager.AddToRoleAsync(miembro.Usuario, "userfree");
                }

                return Ok("Miembro removido exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al remover miembro");
                return StatusCode(500, "Error interno");
            }
        }

        // PUT: api/Suscripciones/{id} - Cancelar suscripción
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> CancelarSuscripcion(int id)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                var suscripcion = await _context.Suscripciones
                    .Include(s => s.Miembros.Where(m => m.EsActivo))
                        .ThenInclude(m => m.Usuario)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (suscripcion == null)
                    return NotFound("Suscripción no encontrada");

                var roles = await _userManager.GetRolesAsync(usuario);
                if (!roles.Contains("admin") && suscripcion.UsuarioId != usuario.Id)
                    return Forbid();

                if (!suscripcion.EsActiva)
                    return BadRequest("La suscripción ya está cancelada");

                // Cancelar suscripción
                suscripcion.EsActiva = false;
                suscripcion.FechaFin = DateTime.Now;

                // Cancelar miembros familiares
                if (suscripcion.Miembros != null)
                {
                    foreach (var miembro in suscripcion.Miembros)
                    {
                        miembro.EsActivo = false;
                        var rolesMiembro = await _userManager.GetRolesAsync(miembro.Usuario);
                        if (rolesMiembro.Contains("userpremium"))
                        {
                            await _userManager.RemoveFromRoleAsync(miembro.Usuario, "userpremium");
                            await _userManager.AddToRoleAsync(miembro.Usuario, "userfree");
                        }
                    }
                }

                // Cambiar rol usuario principal
                var usuarioSuscripcion = await _userManager.FindByIdAsync(suscripcion.UsuarioId.ToString());
                if (usuarioSuscripcion != null)
                {
                    var rolesUsuario = await _userManager.GetRolesAsync(usuarioSuscripcion);
                    if (rolesUsuario.Contains("userpremium"))
                    {
                        await _userManager.RemoveFromRoleAsync(usuarioSuscripcion, "userpremium");
                        await _userManager.AddToRoleAsync(usuarioSuscripcion, "userfree");
                    }
                }

                await _context.SaveChangesAsync();
                return Ok("Suscripción cancelada con éxito");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar suscripción");
                return StatusCode(500, "Error interno");
            }
        }
    }
}