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