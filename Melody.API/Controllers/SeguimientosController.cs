using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeguimientosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SeguimientosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Seguimientos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Seguimiento>>> GetSeguimiento()
        {
            return await _context.Seguimientos.ToListAsync();
        }

        // GET: api/Seguimientos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Seguimiento>> GetSeguimiento(int id)
        {
            var seguimiento = await _context.Seguimientos.FindAsync(id);

            if (seguimiento == null)
            {
                return NotFound();
            }

            return seguimiento;
        }

        // PUT: api/Seguimientos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSeguimiento(int id, Seguimiento seguimiento)
        {
            if (id != seguimiento.Id)
            {
                return BadRequest();
            }

            _context.Entry(seguimiento).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SeguimientoExists(id))
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

        // POST: api/Seguimientos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Seguimiento>> PostSeguimiento(Seguimiento seguimiento)
        {
            _context.Seguimientos.Add(seguimiento);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSeguimiento", new { id = seguimiento.Id }, seguimiento);
        }

        // DELETE: api/Seguimientos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSeguimiento(int id)
        {
            var seguimiento = await _context.Seguimientos.FindAsync(id);
            if (seguimiento == null)
            {
                return NotFound();
            }

            _context.Seguimientos.Remove(seguimiento);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SeguimientoExists(int id)
        {
            return _context.Seguimientos.Any(e => e.Id == id);
        }
    }
}
