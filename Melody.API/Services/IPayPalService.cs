namespace Melody.API.Services
{
    public interface IPayPalService
    {
        Task<(string orderId, string approvalUrl)> CrearOrdenAsync(double monto, string moneda = "USD");
        Task<bool> CapturarPagoAsync(string orderId);
        Task<dynamic> ObtenerDetallesOrdenAsync(string orderId);
    }
}