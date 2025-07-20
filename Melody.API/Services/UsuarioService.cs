using Melody.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Melody.API.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuarioService(UserManager<Usuario> userManager, AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _userManager.FindByIdAsync(userId.ToString());
        }

        public async Task<Artista?> ObtenerArtistaActualAsync()
        {
            var usuario = await ObtenerUsuarioActualAsync();
            if (usuario == null) return null;

            return await _context.Artistas.FirstOrDefaultAsync(a => a.UsuarioId == usuario.Id);
        }
    }
}