using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Melody.Modelos.DTOs;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuscripcionesController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<SuscripcionesController> _logger;

        public SuscripcionesController(AppDbContext context, UserManager<Usuario> userManager, ILogger<SuscripcionesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Helper method para obtener usuario actual
        private async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _userManager.FindByIdAsync(userId.ToString());
        }

        // GET: api/Suscripciones
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerSuscripciones()
        {
            try
            {
                var suscripciones = await _context.Suscripciones
                    .Include(s => s.Usuario)
                    .Include(s => s.Plan)
                    .Include(s => s.Pagos)
                    .Select(s => new
                    {
                        s.Id,
                        s.FechaInicio,
                        s.FechaFin,
                        s.EsActiva,
                        Usuario = new
                        {
                            s.Usuario!.Id,
                            s.Usuario.Nombre,
                            s.Usuario.Apellido,
                            s.Usuario.Email
                        },
                        Plan = new
                        {
                            s.Plan!.Id,
                            s.Plan.Nombre,
                            s.Plan.Precio
                        },
                        TotalPagos = s.Pagos != null ? s.Pagos.Sum(p => p.Monto) : 0,
                        DiasRestantes = s.EsActiva ? Math.Max(0, (s.FechaFin - DateTime.Now).Days) : 0
                    })
                    .OrderByDescending(s => s.FechaInicio)
                    .ToListAsync();

                return Ok(suscripciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener suscripciones");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener suscripciones");
            }
        }

        // GET: api/Suscripciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Suscripcion>> ObtenerSuscripcion(int id)
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var suscripcion = await _context.Suscripciones
                    .Include(s => s.Usuario)
                    .Include(s => s.Plan)
                    .Include(s => s.Pagos)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (suscripcion == null)
                {
                    return NotFound("Suscripción no encontrada");
                }

                // Verificar permisos: admin o dueño de la suscripción
                var roles = await _userManager.GetRolesAsync(usuario);
                if (!roles.Contains("admin") && suscripcion.UsuarioId != usuario.Id)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "No tienes permisos para ver esta suscripción");
                }

                var resultado = new
                {
                    suscripcion.Id,
                    suscripcion.FechaInicio,
                    suscripcion.FechaFin,
                    suscripcion.EsActiva,
                    Usuario = new
                    {
                        suscripcion.Usuario!.Id,
                        suscripcion.Usuario.Nombre,
                        suscripcion.Usuario.Apellido,
                        suscripcion.Usuario.Email
                    },
                    Plan = new
                    {
                        suscripcion.Plan!.Id,
                        suscripcion.Plan.Nombre,
                        suscripcion.Plan.Descripcion,
                        suscripcion.Plan.Precio,
                        suscripcion.Plan.DuracionDias
                    },
                    Pagos = suscripcion.Pagos?.Select(p => new
                    {
                        p.Id,
                        p.Monto,
                        p.FechaPago,
                        p.MetodoPago
                    }).OrderByDescending(p => p.FechaPago).ToList(),
                    DiasRestantes = suscripcion.EsActiva ? Math.Max(0, (suscripcion.FechaFin - DateTime.Now).Days) : 0,
                    TotalPagado = suscripcion.Pagos?.Sum(p => p.Monto) ?? 0
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la suscripción con ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener la suscripción");
            }
        }

        // GET: api/Suscripciones/mi-suscripcion - Obtener mi suscripción activa
        [HttpGet("mi-suscripcion")]
        [Authorize]
        public async Task<ActionResult<object>> ObtenerMiSuscripcion()
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var suscripcionActiva = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Include(s => s.Pagos)
                    .Where(s => s.UsuarioId == usuario.Id && s.EsActiva)
                    .FirstOrDefaultAsync();

                if (suscripcionActiva == null)
                {
                    return Ok(new
                    {
                        mensaje = "No tienes una suscripción activa",
                        tienesSuscripcion = false
                    });
                }

                var resultado = new
                {
                    tienesSuscripcion = true,
                    suscripcion = new
                    {
                        suscripcionActiva.Id,
                        suscripcionActiva.FechaInicio,
                        suscripcionActiva.FechaFin,
                        Plan = new
                        {
                            suscripcionActiva.Plan!.Id,
                            suscripcionActiva.Plan.Nombre,
                            suscripcionActiva.Plan.Descripcion,
                            suscripcionActiva.Plan.Precio
                        },
                        DiasRestantes = Math.Max(0, (suscripcionActiva.FechaFin - DateTime.Now).Days),
                        ProximoVencimiento = suscripcionActiva.FechaFin,
                        UltimoPago = suscripcionActiva.Pagos?.OrderByDescending(p => p.FechaPago).FirstOrDefault()
                    }
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la suscripción del usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener la suscripción");
            }
        }


        // PUT: api/Suscripciones/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> CancelarSuscripcion(int id, Suscripcion suscripcion)
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var suscription = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (suscription == null)
                {
                    return NotFound("Suscripción no encontrada");
                }

                // Verificar permisos: admin o dueño de la suscripción
                var roles = await _userManager.GetRolesAsync(usuario);
                if (!roles.Contains("admin") && suscription.UsuarioId != usuario.Id)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "No tienes permisos para cancelar esta suscripción");
                }

                if (!suscription.EsActiva)
                {
                    return BadRequest("La suscripción ya está cancelada");
                }

                suscription.EsActiva = false;
                suscription.FechaFin = DateTime.Now;

                await _context.SaveChangesAsync();

                // Cambiar rol del usuario de vuelta a userfree
                var usuarioSuscripcion = await _userManager.FindByIdAsync(suscription.UsuarioId.ToString());
                if (usuarioSuscripcion != null)
                {
                    var rolesUsuario = await _userManager.GetRolesAsync(usuarioSuscripcion);
                    if (rolesUsuario.Contains("userpremium"))
                    {
                        await _userManager.RemoveFromRoleAsync(usuarioSuscripcion, "userpremium");
                        await _userManager.AddToRoleAsync(usuarioSuscripcion, "userfree");
                    }
                }

                _logger.LogInformation("Suscripción {Id} cancelada", suscription.Id);

                return Ok(new
                {
                    mensaje = "Suscripción cancelada con éxito",
                    fechaCancelacion = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar la suscripción");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al cancelar la suscripción");
            }
        }

        [HttpPut("{id}/renovar")]
        [Authorize]
        public async Task<IActionResult> RenovarSuscripcion(int id)
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var suscripcion = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuario.Id);

                if (suscripcion == null)
                {
                    return NotFound("Suscripción no encontrada o no tienes permisos");
                }

                if (suscripcion.EsActiva)
                {
                    return BadRequest("La suscripción ya está activa");
                }

                // Verificar que no haya otra suscripción activa
                var otraSuscripcionActiva = await _context.Suscripciones
                    .FirstOrDefaultAsync(s => s.UsuarioId == usuario.Id && s.EsActiva && s.Id != id);

                if (otraSuscripcionActiva != null)
                {
                    return BadRequest("Ya tienes otra suscripción activa");
                }

                suscripcion.EsActiva = true;
                suscripcion.FechaInicio = DateTime.Now;
                suscripcion.FechaFin = DateTime.Now.AddDays(suscripcion.Plan!.DuracionDias);

                await _context.SaveChangesAsync();

                // Cambiar rol del usuario a userpremium
                var rolesActuales = await _userManager.GetRolesAsync(usuario);
                if (rolesActuales.Contains("userfree"))
                {
                    await _userManager.RemoveFromRoleAsync(usuario, "userfree");
                    await _userManager.AddToRoleAsync(usuario, "userpremium");
                }

                _logger.LogInformation("Suscripción {Id} renovada", suscripcion.Id);

                return Ok(new
                {
                    mensaje = "Suscripción renovada con éxito",
                    suscripcion = new
                    {
                        suscripcion.Id,
                        suscripcion.FechaInicio,
                        suscripcion.FechaFin,
                        DiasRestantes = suscripcion.Plan.DuracionDias
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al renovar la suscripción");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al renovar la suscripción");
            }
        }

  

        // GET: api/Suscripciones/historial - Obtener mi historial de suscripciones
        [HttpGet("historial")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerHistorialSuscripciones()
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized("Usuario no autenticado");
                }

                var historial = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Include(s => s.Pagos)
                    .Where(s => s.UsuarioId == usuario.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.FechaInicio,
                        s.FechaFin,
                        s.EsActiva,
                        Plan = new
                        {
                            s.Plan!.Id,
                            s.Plan.Nombre,
                            s.Plan.Precio
                        },
                        TotalPagado = s.Pagos != null ? s.Pagos.Sum(p => p.Monto) : 0,
                        DuracionDias = (s.FechaFin - s.FechaInicio).Days
                    })
                    .OrderByDescending(s => s.FechaInicio)
                    .ToListAsync();

                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de suscripciones");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener historial");
            }
        }
        // GET: api/Suscripciones/estadisticas - Estadísticas de suscripciones (admin)
        [HttpGet("estadisticas")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<object>> ObtenerEstadisticasSuscripciones()
        {
            try
            {
                var totalSuscripciones = await _context.Suscripciones.CountAsync();
                var suscripcionesActivas = await _context.Suscripciones.CountAsync(s => s.EsActiva);
                var suscripcionesCanceladas = totalSuscripciones - suscripcionesActivas;

                var ingresosTotales = await _context.Pagos
                    .SumAsync(p => p.Monto);

                var suscripcionesPorPlan = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .GroupBy(s => s.PlanId)
                    .Select(g => new
                    {
                        PlanId = g.Key,
                        NombrePlan = g.First().Plan!.Nombre,
                        TotalSuscripciones = g.Count(),
                        SuscripcionesActivas = g.Count(s => s.EsActiva)
                    })
                    .OrderByDescending(x => x.TotalSuscripciones)
                    .ToListAsync();

                var suscripcionesPorMes = await _context.Suscripciones
                    .GroupBy(s => new { s.FechaInicio.Year, s.FechaInicio.Month })
                    .Select(g => new
                    {
                        Año = g.Key.Year,
                        Mes = g.Key.Month,
                        TotalSuscripciones = g.Count()
                    })
                    .OrderByDescending(x => x.Año)
                    .ThenByDescending(x => x.Mes)
                    .Take(12)
                    .ToListAsync();

                return Ok(new
                {
                    TotalSuscripciones = totalSuscripciones,
                    SuscripcionesActivas = suscripcionesActivas,
                    SuscripcionesCanceladas = suscripcionesCanceladas,
                    IngresosTotales = ingresosTotales,
                    TasaRetencion = totalSuscripciones > 0 ?
                        Math.Round((double)suscripcionesActivas / totalSuscripciones * 100, 2) : 0,
                    SuscripcionesPorPlan = suscripcionesPorPlan,
                    SuscripcionesPorMes = suscripcionesPorMes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de suscripciones");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener estadísticas");
            }
        }
    }
}