namespace Melody.API.Services
{
    public interface IAudioService
    {
        TimeSpan? ObtenerDuracionAudio(IFormFile archivoAudio);
    }
}