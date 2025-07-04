using Melody.API.Service;
using Melody.Modelos.DTOs;
using Melody.Modelos;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            JwtService jwtService,
            EmailService emailService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("registro")]
        public async Task<IActionResult> Registrar([FromBody] RegistroDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validamos que ambas contraseñas sean iguales
                if (dto.Password != dto.ConfirmarPassword)
                    return BadRequest(new { error = "Las contraseñas no coinciden" });

                // Verificamos si el email ingresado ya esta registrado
                if (await _userManager.FindByEmailAsync(dto.Email) != null)
                    return BadRequest(new { error = "Este correo ya está registrado, prueba con otro." });

                // Creaamos el usuario
                var usuario = new Usuario
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    Nombre = dto.Nombre,
                    Apellido = dto.Apellido,
                    EmailConfirmed = false
                };

                var resultado = await _userManager.CreateAsync(usuario, dto.Password);
                if (!resultado.Succeeded)
                {
                    var errores = resultado.Errors.Select(e => e.Description);
                    return BadRequest(new { errores });
                }

                //Verficamos si se registro como artista
                string rol = dto.EsArtista ? "artista" : "userfree";
                await _userManager.AddToRoleAsync(usuario, rol);

                // Generar token de confirmación
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var confirmationLink = Url.Action("ConfirmarEmail", "Auth",
                    new { userId = usuario.Id, token = encodedToken }, Request.Scheme);

                // Enviar email de confirmación
                await _emailService.EnviarEmailAsync(dto.Email, "Confirma tu cuenta en Melody Stream",
                    $"¡Da clic en este enlace y forma parte de la familia Melody Stream!: {confirmationLink}");

                _logger.LogInformation("Usuario registrado: {Email}", dto.Email);

                return Ok(new { mensaje = "¡Te enviamos un enlace de confirmación a tu correo! Confirmala y se parte de la Familia Melody Stream." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario");
                return StatusCode(500, new { error = "Error" });
            }
        }

        [HttpGet("confirmar-email")]
        public async Task<IActionResult> ConfirmarEmail(int userId, string token)
        {
            try
            {
                var usuario = await _userManager.FindByIdAsync(userId.ToString());
                if (usuario == null)
                    return BadRequest(new { error = "Usuario no encontrado" });

                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                var resultado = await _userManager.ConfirmEmailAsync(usuario, decodedToken);

                if (resultado.Succeeded)
                {
                    _logger.LogInformation("Email confirmado para: {Email}", usuario.Email);
                    return Ok(new { mensaje = "Email confirmado exitosamente. Ya puedes iniciar sesión y disfrutar de nuestra Aplilcación web " });
                }

                return BadRequest(new { error = "Error al confirmar el email. El token puede haber expirado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar email");
                return BadRequest(new { error = "Token inválido o expirado" });
            }
        }
    }
}
