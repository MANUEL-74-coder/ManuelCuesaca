using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Melody.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Melody.API.Services
{
    public class PdfService : IPdfService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PdfService> _logger;

        public PdfService(AppDbContext context, ILogger<PdfService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<byte[]> GenerarComprobantePagoAsync(int pagoId)
        {
            try
            {
                _logger.LogInformation("Generando PDF para pago {PagoId}", pagoId);

                var pago = await _context.Pagos
                    .Include(p => p.Suscripcion)
                        .ThenInclude(s => s.Usuario)
                    .Include(p => p.Suscripcion)
                        .ThenInclude(s => s.Plan)
                    .FirstOrDefaultAsync(p => p.Id == pagoId);

                if (pago == null)
                {
                    throw new ArgumentException($"Pago con ID {pagoId} no encontrado");
                }

                // Validaciones de datos
                if (pago.Suscripcion?.Usuario == null || pago.Suscripcion?.Plan == null)
                {
                    throw new InvalidDataException("Datos del pago incompletos");
                }

                // Crear PDF con manejo correcto de recursos
                var pdfBytes = CrearPdfBytes(pago);

                _logger.LogInformation("PDF generado exitosamente. Tamaño: {Size} bytes", pdfBytes.Length);
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF para pago {PagoId}", pagoId);
                throw;
            }
        }

        private byte[] CrearPdfBytes(Pago pago)
        {
            using var stream = new MemoryStream();

            // Importante: NO usar using con PdfWriter, PdfDocument y Document aquí
            // porque necesitamos que el stream se complete antes de cerrarse

            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            try
            {
                // Título principal
                document.Add(new Paragraph("MELODY MUSIC")
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(5));

                document.Add(new Paragraph("COMPROBANTE DE PAGO")
                    .SetFontSize(16)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                // Información del comprobante
                document.Add(new Paragraph($"Comprobante N°: PAGO-{pago.Id:000}")
                    .SetFontSize(12)
                    .SetMarginBottom(5));

                document.Add(new Paragraph($"Fecha: {pago.FechaPago:dd/MM/yyyy HH:mm}")
                    .SetMarginBottom(15));

                // Separador
                document.Add(new Paragraph("=")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));

                // Información del cliente
                document.Add(new Paragraph("DATOS DEL CLIENTE")
                    .SetFontSize(14)
                    .SetMarginBottom(5));

                document.Add(new Paragraph($"Nombre: {pago.Suscripcion.Usuario.Nombre ?? ""} {pago.Suscripcion.Usuario.Apellido ?? ""}")
                    .SetMarginBottom(3));

                document.Add(new Paragraph($"Email: {pago.Suscripcion.Usuario.Email ?? ""}")
                    .SetMarginBottom(15));

                // Información del plan
                document.Add(new Paragraph("DETALLES DEL SERVICIO")
                    .SetFontSize(14)
                    .SetMarginBottom(5));

                document.Add(new Paragraph($"Plan: {pago.Suscripcion.Plan.Nombre ?? ""}")
                    .SetMarginBottom(3));

                if (!string.IsNullOrEmpty(pago.Suscripcion.Plan.Descripcion))
                {
                    document.Add(new Paragraph($"Descripción: {pago.Suscripcion.Plan.Descripcion}")
                        .SetMarginBottom(3));
                }

                document.Add(new Paragraph($"Vigencia: {pago.Suscripcion.FechaInicio:dd/MM/yyyy} - {pago.Suscripcion.FechaFin:dd/MM/yyyy}")
                    .SetMarginBottom(15));

                // Separador
                document.Add(new Paragraph("=")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));

                // Información del pago
                document.Add(new Paragraph("INFORMACIÓN DEL PAGO")
                    .SetFontSize(14)
                    .SetMarginBottom(5));

                document.Add(new Paragraph($"Método de Pago: {pago.MetodoPago ?? "PayPal"}")
                    .SetMarginBottom(3));

                document.Add(new Paragraph($"Estado: COMPLETADO")
                    .SetMarginBottom(10));

                // Monto total destacado
                document.Add(new Paragraph($"TOTAL PAGADO: ${pago.Monto:F2} USD")
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(15)
                    .SetMarginBottom(20));

                // Beneficios
                document.Add(new Paragraph("BENEFICIOS INCLUIDOS:")
                    .SetFontSize(14)
                    .SetMarginBottom(5));

                document.Add(new Paragraph("• Gestión completa de playlists"));
                document.Add(new Paragraph("• Descargas ilimitadas"));
                document.Add(new Paragraph($"• Hasta {(pago.Suscripcion.Plan.NumeroUsuarios > 0 ? pago.Suscripcion.Plan.NumeroUsuarios.ToString() : "ilimitados")} usuarios"));
                document.Add(new Paragraph("• Calidad de audio premium"));
                document.Add(new Paragraph("• Soporte técnico prioritario"));

                // Mensaje final
                document.Add(new Paragraph("")
                    .SetMarginTop(20));

                document.Add(new Paragraph("¡Gracias por elegir Melody Music!")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(16)
                    .SetMarginBottom(10));

                document.Add(new Paragraph("Disfruta de tu experiencia musical premium")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                // Pie de página
                document.Add(new Paragraph("-")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(5));

                document.Add(new Paragraph($"Documento generado el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(8));

                document.Add(new Paragraph("Este es un documento válido generado electrónicamente")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(8));
            }
            finally
            {
                // Cerrar en el orden correcto
                document.Close();
                pdf.Close();
                writer.Close();
            }

            return stream.ToArray();
        }
    }
}