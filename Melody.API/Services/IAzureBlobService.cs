namespace Melody.API.Services
{
    public interface IAzureBlobService
    {
        Task<string> SubirArchivoAsync(IFormFile archivo, string contenedor, string prefijo = "");
        Task EliminarArchivoAsync(string url, string contenedor);
    }
}