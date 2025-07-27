using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Melody.Modelos.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeguimientosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public SeguimientosController(AppDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper method para obtener usuario actual
        private async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _userManager.FindByIdAsync(userId.ToString());
        }

        // GET: api/Seguimientos
        [HttpGet("mis-seguimientos")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Seguimiento>>> ObtenerMisSeguimientos()
        {
            try
            {
                var usuario = await ObtenerUsuarioActualAsync();
                if (usuario == null)
                {
                    return Unauthorized();
                }
                var seguimientos = await _context.Seguimientos
                    .Include(s => s.Artista)
                    .ThenInclude(a => a.Usuario)
                    .Where(s => s.UsuarioId == usuario.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.FechaSeguimiento,
                        Artista = new
                        {
                            s.Artista!.Id,
                            s.Artista.NombreArtista,
                            s.Artista.ImagenPerfil,
                            s.Artista.Biografia,
                            FechaRegistro = s.Artista.Usuario!.FechaRegistro
                        }
                    })
                    .OrderByDescending(s => s.FechaSeguimiento)
                    .ToListAsync();
                return Ok(seguimientos);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener seguimientos");
            }
        }

        // POST: api/Seguimientos/toogle - Toggle seguir/dejar de seguir artista
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("toggle")]
        [Authorize()]
        public async Task<ActionResult<object>> ToogleSeguirArtista([FromBody] SeguirArtistaDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var usuarioActual = await ObtenerUsuarioActualAsync();
                if (usuarioActual == null)
                {
                    return Unauthorized("Usuario no autenticado.");
                }

                // Verificar si el artista existe
                var artista = await _context.Artistas.FindAsync(dto.ArtistaId);
                if (artista == null)
                {
                    return NotFound("Artista no encontrado.");
                }

                // Verificar que no se esté siguiendo a sí mismo ( si es artista)
                var artistaUsuario = await _context.Artistas.FirstOrDefaultAsync(a => a.UsuarioId == usuarioActual.Id);
                if (artistaUsuario != null && artistaUsuario.Id == dto.ArtistaId)
                {
                    return BadRequest("No puedes seguirte a ti mismo como artista.");
                }

                //Buscar si ya existe el seguimiento
                var seguimientoExistente = await _context.Seguimientos
                    .FirstOrDefaultAsync(s => s.UsuarioId == usuarioActual.Id && s.ArtistaId == dto.ArtistaId);
                if (seguimientoExistente != null)
                {
                    //Ya lo sigue, entonces dejar de seguir
                    _context.Seguimientos.Remove(seguimientoExistente);
                    await _context.SaveChangesAsync();
                    return Ok(new
                    {
                        accion = "dejar_de_seguir",
                        mensaje = "Has dejado de seguir a este artista",
                        siguiendo = false,
                        artista = new
                        {
                            artista.Id,
                            artista.NombreArtista
                        }
                    });

                }
                else
                {
                    //No lo sigue, entonces seguir
                    var nuevoSeguimiento = new Seguimiento
                    {
                        UsuarioId = usuarioActual.Id,
                        ArtistaId = dto.ArtistaId,
                        FechaSeguimiento = DateTime.Now,
                    };
                    _context.Seguimientos.Add(nuevoSeguimiento);
                    await _context.SaveChangesAsync();
                    return Ok(new
                    {
                        accion = "seguir",
                        mensaje = "Ahora sigues a este artista",
                        siguiendo = true,
                        artista = new
                        {
                            artista.Id,
                            artista.NombreArtista
                        },
                        fechaSeguimiento = nuevoSeguimiento.FechaSeguimiento

                    });
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error al seguir al artista: {ex.Message}");
            }
        }
    }
}
