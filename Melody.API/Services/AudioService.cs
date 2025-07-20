namespace Melody.API.Services
{
    public class AudioService : IAudioService
    {
        private readonly ILogger<AudioService> _logger;

        public AudioService(ILogger<AudioService> logger)
        {
            _logger = logger;
        }

        public TimeSpan? ObtenerDuracionAudio(IFormFile archivoAudio)
        {
            try
            {
                using var stream = archivoAudio.OpenReadStream();
                using var reader = new NAudio.Wave.Mp3FileReader(stream);
                return reader.TotalTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener duración de {FileName}", archivoAudio.FileName);
                return null;
            }
        }
    }
}