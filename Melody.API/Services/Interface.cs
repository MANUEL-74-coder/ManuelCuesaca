using Melody.Modelos;

namespace Melody.API.Services
{
    public interface IUsuarioService
    {
        Task<Usuario?> ObtenerUsuarioActualAsync();
        Task<Artista?> ObtenerArtistaActualAsync();
    }
}