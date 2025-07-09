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

        public async Task EnviarEmailForgotPasswordAsync(string destinatario, string nombreUsuario, string resetLink)
        {
            var asunto = "Restablecer Contraseña - Melody Stream";
            var contenido = $@"Hola {nombreUsuario},
            Recibimos una solicitud para restablecer la contraseña de tu cuenta en Melody Stream.
            Para restablecer tu contraseña, haz clic en el siguiente enlace:
            {resetLink} 
            Este enlace expirará en 30 minutos por motivos de seguridad.    
            Si no solicitaste este cambio, simplemente ignora este correo.
            Saludos,
            El equipo de Melody Stream";
            await EnviarEmailAsync(destinatario, asunto, contenido);
        }

        public async Task EnviarEmailPasswordResetConfirmationAsync(string destinatario, string nombreUsuario)
        {
            var asunto = "Contraseña Restablecida - Melody Stream";
            var contenido = $@"Hola {nombreUsuario},    
            Tu contraseña ha sido restablecida exitosamente.
            Ya puedes iniciar sesión en Melody Stream con tu nueva contraseña.  
            IMPORTANTE: Si no realizaste este cambio, contacta inmediatamente con nuestro soporte.
            Saludos,
            El equipo de Melody Stream";
            await EnviarEmailAsync(destinatario, asunto, contenido);
        }

    }
}
