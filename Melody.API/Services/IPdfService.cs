namespace Melody.API.Services
{
    public interface IPdfService
    {
        Task<byte[]> GenerarComprobantePagoAsync(int pagoId);
    }
}