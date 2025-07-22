using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Authorization;
using NuGet.Protocol;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class GenerosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GenerosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Generos
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Genero>>> ObtenerGeneros()
        {
            var generos = await _context.Generos
                .Select(g => new
                {
                    g.Id,
                    g.Nombre,
                    TotalAlbums = g.Albums != null ? g.Albums.Count : 0,
                    TotalCanciones = g.Canciones != null ? g.Canciones.Count : 0
                })
                .ToListAsync();
            return Ok(generos);
        }

        // GET: api/Generos/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Genero>> ObtenerGenero(int id)
        {
            var genero = await _context.Generos
                .AsSplitQuery()
                .Include(g => g.Albums)
                    .ThenInclude(a => a.Artista)
                .Include(g => g.Canciones)
                    .ThenInclude(c => c.Artista)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (genero == null)
            {
                return NotFound();
            }
            return genero;
        }

        // PUT: api/Generos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ActualizarGenero(int id, Genero genero)
        {
            if (id != genero.Id)
            {
                return BadRequest();
            }
            // Verificar si el género existe
            var existingGenero = await _context.Generos.FindAsync(id);
            if (existingGenero == null)
            {
                return NotFound();
            }
            //Verificar si el nombre del género ya existe
            var nombreExistente = await _context.Generos
                .FirstOrDefaultAsync(g => g.Nombre.ToLower() == genero.Nombre.ToLower() && g.Id != id);
            if (nombreExistente != null)
                return BadRequest("Ya existe un género con el mismo nombre.");

            existingGenero.Nombre = genero.Nombre;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GeneroExists(id))
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

        // POST: api/Generos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<Genero>> CrearGenero(Genero genero)
        {
            // Verificar si el nombre del género ya existe
            var nombreExistente = await _context.Generos
                .FirstOrDefaultAsync(g => g.Nombre.ToLower() == genero.Nombre.ToLower());
            if (nombreExistente != null)
                return BadRequest("Ya existe un género con el mismo nombre.");
            _context.Generos.Add(genero);
            await _context.SaveChangesAsync();

            return CreatedAtAction("ObtenerGenero", new { id = genero.Id }, genero);
        }

        // DELETE: api/Generos/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EliminarGenero(int id)
        {
            try
            {
                var genero = await _context.Generos
                    .Include(g => g.Albums)
                    .Include(g => g.Canciones)
                    .FirstOrDefaultAsync(g => g.Id == id);
                if (genero == null)
                {
                    return NotFound();
                }
                //Verificar si el género tiene álbumes o canciones
                if ((genero.Albums != null && genero.Albums.Count > 0) || (genero.Canciones != null && genero.Canciones.Count > 0))
                {
                    return BadRequest("No se puede eliminar el género porque tiene álbumes o canciones asociados.");
                }
                _context.Generos.Remove(genero);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error al eliminar el género: {ex.Message}");
            }
        }

        private bool GeneroExists(int id)
        {
            return _context.Generos.Any(e => e.Id == id);
        }
    }
}
