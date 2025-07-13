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
        private readonly AppDbContext _appDbContext;


        public AuthController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            JwtService jwtService,
            EmailService emailService,
            ILogger<AuthController> logger,
            AppDbContext appDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
            _appDbContext = appDbContext;
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


                // Si se registró como artista, creamos automáticamente su perfil
                if (dto.EsArtista)
                {
                    var artista = new Artista
                    {
                        NombreArtista = $"{dto.Nombre} {dto.Apellido}", // Nombre completo como nombre artístico inicial
                        Biografia = null, // Se completará después
                        ImagenPerfil = null, // Se completará después
                        UsuarioId = usuario.Id
                    };

                    _appDbContext.Artistas.Add(artista);
                    await _appDbContext.SaveChangesAsync();

                    _logger.LogInformation("Perfil de artista creado para usuario: {Email}", dto.Email);
                }


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
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var usuario = await _userManager.FindByEmailAsync(dto.Email);
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de login fallido para: {Email}", dto.Email);
                    return Unauthorized(new { error = "Este email no está registrado" });
                }

                // Verificamos si el email está confirmado
                if (!usuario.EmailConfirmed)
                    return Unauthorized(new { error = "Por favor revisa tu email, te enviamos un correo de confirmación" });

                // Verificamos si la cuenta está bloqueada por múltiples intentos fallidos
                if (await _userManager.IsLockedOutAsync(usuario))
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(usuario);
                    var remainingTime = lockoutEnd?.Subtract(DateTimeOffset.Now);
                    return Unauthorized(new { error = $"Tu cuenta está bloqueada. Intenta en {remainingTime?.Minutes} minutos" });
                }

                var resultado = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password, false, true);
                if (!resultado.Succeeded)
                {
                    await _userManager.AccessFailedAsync(usuario);
                    return Unauthorized(new { error = "Contraseña incorrecta" });
                }

                // Reestablecemos el contador de intentos fallidos
                await _userManager.ResetAccessFailedCountAsync(usuario);

                // Generar token JWT
                var roles = await _userManager.GetRolesAsync(usuario);
                var token = _jwtService.GenerarToken(usuario, roles);

                _logger.LogInformation("Login exitoso para: {Email}", dto.Email);

                return Ok(new { mensaje = "Login exitoso", token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login");
                return StatusCode(500, new { error = "Error" });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var usuario = await _userManager.FindByEmailAsync(dto.Email);
                if (usuario == null || !await _userManager.IsEmailConfirmedAsync(usuario))
                {
                    return Ok(new { mensaje = "Si tu correo está registrado, recibirás un enlace para restablecer la contraseña." });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var resetPasswordUrl = "https://localhost:7291"; // Tu puerto del MVC
                var resetLink = $"{resetPasswordUrl}/Auth/ResetPassword?email={Uri.EscapeDataString(dto.Email)}" +
                                $"&token={Uri.EscapeDataString(encodedToken)}";



                await _emailService.EnviarEmailForgotPasswordAsync(dto.Email, usuario.Nombre, resetLink);

                _logger.LogInformation("Enlace de cambio de contraseña enviado a: {Email}", dto.Email);

                return Ok(new { mensaje = "Si tu correo está registrado, recibirás un enlace para restablecer la contraseña." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar enlace de cambio de contraseña");
                return StatusCode(500, new { error = "Ocurrió un error. Intenta nuevamente." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validamos que ambas contraseñas sean iguales
                if (dto.NuevaPassword != dto.ConfirmarPassword)
                    return BadRequest(new { error = "Las contraseñas no coinciden" });


                var usuario = await _userManager.FindByEmailAsync(dto.Email);
                if (usuario == null)
                    return BadRequest(new { error = "No se pudo verificar el usuario." });

                string decodedToken;
                try
                {
                    decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));
                }
                catch
                {
                    return BadRequest(new { error = "El enlace de restablecimiento es inválido o ha expirado." });
                }

                // Verificamos si el token es válido
                var isValidToken = await _userManager.VerifyUserTokenAsync(usuario,
                    _userManager.Options.Tokens.PasswordResetTokenProvider,
                    "ResetPassword",
                    decodedToken);

                if (!isValidToken)
                {
                    _logger.LogWarning("Token de reset inválido o expirado para usuario {Email}", dto.Email);
                    return BadRequest(new { error = "El enlace de restablecimiento es inválido o ha expirado." });
                }

                // Restablecemos la contraseña
                var resultado = await _userManager.ResetPasswordAsync(usuario, decodedToken, dto.NuevaPassword);

                if (!resultado.Succeeded)
                {
                    var errores = resultado.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { error = "No se pudo cambiar la contraseña.", errores });
                }

                await _emailService.EnviarEmailPasswordResetConfirmationAsync(usuario.Email, usuario.Nombre);

                _logger.LogInformation("Contraseña restablecida exitosamente para: {Email}", dto.Email);

                return Ok(new { mensaje = "¡Contraseña restablecida exitosamente! Ya puedes iniciar sesión con tu nueva contraseña." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña");
                return StatusCode(500, new { error = "Ocurrió un error. Intenta nuevamente." });
            }
        }
    }
}