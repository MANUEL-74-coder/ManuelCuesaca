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

        // GET: api/Planes/5
        [HttpGet("{id}")]
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
        public async Task<IActionResult> ActualizarPlan(int id, Plan plan)
        {
            if (id != plan.Id)
            {
                return BadRequest();
            }

            var planExistente = await _context.Planes.FindAsync(id);
            if (planExistente == null)
            {
                return NotFound();
            }
            // Verificar si el nombre del plan ya existe
            var planConMismoNombre = await _context.Planes
                .Where(p => p.Id != id && p.Nombre.ToLower() == plan.Nombre.ToLower())
                .FirstOrDefaultAsync();
            if (planConMismoNombre != null)
            {
                return BadRequest("Ya existe un plan con ese nombre.");
            }

            _context.Entry(plan).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlanExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
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
                var nuevoPlan = new Plan
                {
                    Nombre = plan.Nombre,
                    Descripcion = plan.Descripcion,
                    Precio = plan.Precio,
                    DuracionDias = plan.DuracionDias,
                    NumeroUsuarios = plan.NumeroUsuarios
                };
                _context.Planes.Add(nuevoPlan);
                await _context.SaveChangesAsync();
                return CreatedAtAction("ObtenerPlan", new { id = nuevoPlan.Id }, nuevoPlan);
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
        public async Task<ActionResult<object>> ObtenerEstadisticasPlanes()
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