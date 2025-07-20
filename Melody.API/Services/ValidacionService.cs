namespace Melody.API.Services
{
    public class ValidacionService
    {
        public static class Archivos
        {
            public static readonly string[] ExtensionesAudio = { ".mp3", ".wav", ".flac" };
            public static readonly string[] ExtensionesImagen = { ".jpg", ".jpeg", ".png", ".webp" };
            public const long MaxTamanoAudio = 50 * 1024 * 1024; // 50MB
            public const long MaxTamanoImagen = 5 * 1024 * 1024; // 5MB
        }

        public static (bool esValido, string error) ValidarArchivo(IFormFile archivo, string[] extensionesPermitidas, long tamanoMaximo)
        {
            if (archivo == null || archivo.Length == 0)
                return (false, "Archivo requerido");

            var extension = Path.GetExtension(archivo.FileName).ToLower();
            if (!extensionesPermitidas.Contains(extension))
                return (false, $"Formato no permitido. Use: {string.Join(", ", extensionesPermitidas)}");

            if (archivo.Length > tamanoMaximo)
                return (false, $"El archivo no puede exceder {tamanoMaximo / (1024 * 1024)} MB");

            return (true, string.Empty);
        }
    }
}