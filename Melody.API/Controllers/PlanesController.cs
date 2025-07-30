using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Authorization;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PlanesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Planes
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Plan>>> ObtenerPLanes()
        {
            try
            {
                var planes = await _context.Planes
                    .Select(p => new
                    {
                        p.Id,
                        p.Nombre,
                        p.Descripcion,
                        p.Precio,
                        p.DuracionDias,
                        p.NumeroUsuarios,
                        TotalSuscripciones = p.Suscripciones != null ? p.Suscripciones.Count : 0,
                        SuscripcionesActivasd = p.Suscripciones != null ? p.Suscripciones.Count(s => s.EsActiva) : 0
                    })
                    .OrderBy(p => p.Precio)
                    .ToListAsync();
                return Ok(planes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los planes");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<Plan>> ObtenerPlan(int id)
        {
            try
            {
                var plan = await _context.Planes.FirstOrDefaultAsync(p => p.Id == id);
                if (plan == null)
                {
                    return NotFound();
                }
                var suscripcionesActivas = await _context.Suscripciones
                    .Where(s => s.PlanId == id && s.EsActiva)
                    .CountAsync();
                var totalSuscripciones = await _context.Suscripciones
                    .Where(s => s.PlanId == id)
                    .CountAsync();
                var resultado = new
                {
                    plan.Id,
                    plan.Nombre,
                    plan.Descripcion,
                    plan.Precio,
                    plan.DuracionDias,
                    plan.NumeroUsuarios,
                    TotalSuscripciones = totalSuscripciones,
                    SuscripcionesActivas = suscripcionesActivas
                };
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el plan");
            }
        }

        // PUT: api/Planes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ActualizarPlan(int id, Plan plan)
        {
            try
            {
                if (id != plan.Id)
                {
                    return BadRequest("El ID del plan no coincide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var planExistente = await _context.Planes.FindAsync(id);
                if (planExistente == null)
                {
                    return NotFound("Plan no encontrado");
                }

                // Verificar si el nombre del plan ya existe
                var planConMismoNombre = await _context.Planes
                    .Where(p => p.Id != id && p.Nombre.ToLower() == plan.Nombre.ToLower())
                    .FirstOrDefaultAsync();

                if (planConMismoNombre != null)
                {
                    return BadRequest("Ya existe un plan con ese nombre.");
                }

                planExistente.Nombre = plan.Nombre;
                planExistente.Descripcion = plan.Descripcion;
                planExistente.Precio = plan.Precio;
                planExistente.DuracionDias = plan.DuracionDias;
                planExistente.NumeroUsuarios = plan.NumeroUsuarios;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Plan actualizado con éxito",
                    plan = planExistente
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlanExists(id))
                {
                    return NotFound("Plan no encontrado");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error al actualizar el plan: {ex.Message}");
            }
        }

        // POST: api/Planes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<Plan>> CrearPlan(Plan plan)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                //Verificar si el plan ya existe
                var planExistente = await _context.Planes
                    .FirstOrDefaultAsync(p => p.Nombre.ToLower() == plan.Nombre.ToLower());
                if (planExistente != null)
                {
                    return BadRequest("Ya existe un plan con ese nombre.");
                }
                _context.Planes.Add(plan);
                await _context.SaveChangesAsync();
                return CreatedAtAction("ObtenerPlan", new { id = plan.Id }, plan);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error al crear el plan: {ex.Message}");
            }
        }

        // DELETE: api/Planes/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EliminarPlan(int id)
        {
            try
            {
                var plan = await _context.Planes.
                    Include(p => p.Suscripciones)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (plan == null)
                {
                    return NotFound("Plan no encontrado");
                }

                //Veriifcamos si el plan tiene suscripciones activas
                if (plan.Suscripciones != null && plan.Suscripciones.Any(s => s.EsActiva))
                {
                    return BadRequest("No se puede eliminar un plan que tiene suscripciones activas.");
                }
                _context.Planes.Remove(plan);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Plan eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error al eliminar el plan: {ex.Message}");
            }
        }

        // GET: api/Planes/estadisticas - Estadísticas de planes (admin)
        [HttpGet("estadisticas")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<Plan>> ObtenerEstadisticasPlanes()
        {
            try
            {
                var totalPlanes = await _context.Planes.CountAsync();
                var totalSuscripciones = await _context.Suscripciones.CountAsync();
                var suscripcionesActivas = await _context.Suscripciones.CountAsync(s => s.EsActiva);

                var ingresosPorPlan = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Include(s => s.Pagos)
                    .Where(s => s.EsActiva)
                    .GroupBy(s => s.PlanId)
                    .Select(g => new
                    {
                        PlanId = g.Key,
                        NombrePlan = g.First().Plan!.Nombre,
                        SuscripcionesActivas = g.Count(),
                        IngresosTotales = g.SelectMany(s => s.Pagos!).Sum(p => p.Monto)
                    })
                    .OrderByDescending(x => x.IngresosTotales)
                    .ToListAsync();

                return Ok(new
                {
                    TotalPlanes = totalPlanes,
                    TotalSuscripciones = totalSuscripciones,
                    SuscripcionesActivas = suscripcionesActivas,
                    IngresosPorPlan = ingresosPorPlan,
                    IngresosTotal = ingresosPorPlan.Sum(i => i.IngresosTotales)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener estadísticas");
            }
        }

        private bool PlanExists(int id)
        {
            return _context.Planes.Any(e => e.Id == id);
        }
    }
}
