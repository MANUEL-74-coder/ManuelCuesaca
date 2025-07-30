using Azure.Storage.Blobs;
using Melody.API.Services;

public class AzureBlobService : IAzureBlobService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureBlobService> _logger;
    private readonly string _baseUrl;

    public AzureBlobService(IConfiguration configuration, ILogger<AzureBlobService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _baseUrl = "https://appmelody.blob.core.windows.net";
    }
    // SOLUCIÓN 2: Verificar el método de Azure Blob Service
    public async Task<byte[]> DescargarArchivoAsync(string url)
    {
        try
        {
            _logger.LogInformation("Descargando archivo desde Azure: {Url}", url);

            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("URL de archivo vacía o nula");
                return null;
            }

            var uri = new Uri(url);
            var blobName = uri.Segments.Last();

            _logger.LogInformation("Blob name extraído: {BlobName}", blobName);

            // Para canciones, usar el contenedor "Canciones"
            var sasUrl = _configuration["AzureStorage:Canciones"];

            if (string.IsNullOrEmpty(sasUrl))
            {
                _logger.LogError("SAS URL para contenedor Canciones no configurada");
                return null;
            }

            var containerClient = new BlobContainerClient(new Uri(sasUrl));
            var blobClient = containerClient.GetBlobClient(blobName);

            // Verificar que el blob existe
            var exists = await blobClient.ExistsAsync();
            if (!exists.Value)
            {
                _logger.LogWarning("Archivo no encontrado en Azure: {BlobName}", blobName);
                return null;
            }

            _logger.LogInformation("Blob encontrado, descargando contenido...");

            // Descargar el archivo
            var response = await blobClient.DownloadContentAsync();
            var bytes = response.Value.Content.ToArray();

            _logger.LogInformation("Descarga completada. Tamaño: {Size} bytes", bytes.Length);

            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar archivo desde Azure: {Url}", url);
            return null;
        }
    }

    public async Task<string> SubirArchivoAsync(IFormFile archivo, string contenedor, string prefijo = "")
    {
        var extension = Path.GetExtension(archivo.FileName).ToLower();
        var nombreArchivo = $"{prefijo}_{Guid.NewGuid()}{extension}";

        var sasUrl = _configuration[$"AzureStorage:{contenedor}"];
        var containerClient = new BlobContainerClient(new Uri(sasUrl));
        var blobClient = containerClient.GetBlobClient(nombreArchivo);

        using var stream = archivo.OpenReadStream();
        await blobClient.UploadAsync(stream, true);

        return $"{_baseUrl}/{ObtenerNombreContenedor(contenedor)}/{nombreArchivo}";
    }

    public async Task EliminarArchivoAsync(string url, string contenedor)
    {
        if (string.IsNullOrEmpty(url)) return;
        if (url.Contains("default.jpg") || url.Contains("default-album.jpg") || url.Contains("default-playlist.jpg"))
        {
            return; 
        }
        try
        {
            var uri = new Uri(url);
            var blobName = uri.Segments.Last();

            var sasUrl = _configuration[$"AzureStorage:{contenedor}"];
            var containerClient = new BlobContainerClient(new Uri(sasUrl));
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar archivo: {Url}", url);
        }
    }

    private static string ObtenerNombreContenedor(string contenedor) => contenedor.ToLower() switch
    {
        "canciones" => "canciones",
        "portadas" => "portadas",
        "albums" => "album-images",
        "perfiles" => "perfiles",
        "playlists" => "playlists-images",
        _ => contenedor.ToLower()
    };
}