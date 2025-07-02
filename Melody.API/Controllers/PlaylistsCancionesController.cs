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
    public class PlaylistsCancionesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PlaylistsCancionesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PlaylistsCanciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlaylistCancion>>> GetPlaylistCancion()
        {
            return await _context.PlaylistsCanciones.ToListAsync();
        }

        // GET: api/PlaylistsCanciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlaylistCancion>> GetPlaylistCancion(int id)
        {
            var playlistCancion = await _context.PlaylistsCanciones.FindAsync(id);

            if (playlistCancion == null)
            {
                return NotFound();
            }

            return playlistCancion;
        }

        // PUT: api/PlaylistsCanciones/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlaylistCancion(int id, PlaylistCancion playlistCancion)
        {
            if (id != playlistCancion.Id)
            {
                return BadRequest();
            }

            _context.Entry(playlistCancion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlaylistCancionExists(id))
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

        // POST: api/PlaylistsCanciones
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PlaylistCancion>> PostPlaylistCancion(PlaylistCancion playlistCancion)
        {
            _context.PlaylistsCanciones.Add(playlistCancion);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPlaylistCancion", new { id = playlistCancion.Id }, playlistCancion);
        }

        // DELETE: api/PlaylistsCanciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlaylistCancion(int id)
        {
            var playlistCancion = await _context.PlaylistsCanciones.FindAsync(id);
            if (playlistCancion == null)
            {
                return NotFound();
            }

            _context.PlaylistsCanciones.Remove(playlistCancion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PlaylistCancionExists(int id)
        {
            return _context.PlaylistsCanciones.Any(e => e.Id == id);
        }
    }
}
