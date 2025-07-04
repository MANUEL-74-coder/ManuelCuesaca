using System.Net.Mail;
using System.Net;

namespace Melody.API.Service
{
    public class EmailService
    {

        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarEmailAsync(string destinatario, string asunto, string contenido)
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"];
            // Configuración básica con Gmail
            var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = asunto,
                Body = contenido,
                IsBodyHtml = false,
            };
            mailMessage.To.Add(destinatario);

            await smtpClient.SendMailAsync(mailMessage);
        }

    }
}
