using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Melody.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Si ya está logueado, redirigmos a home
            if (_authService.IsAuthenticated())
            {
                TempData["InfoMessage"] = "Ya tienes una sesión activa.";
                return RedirectToAction("Index", "Home");
            }
            return View(new LoginDto());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var resultado = await _authService.LoginAsync(model);

                if (resultado.IsSuccess)
                {
                    if (!string.IsNullOrEmpty(resultado.Token))
                    {
                        //Guardamos la sesión
                        HttpContext.Session.SetString("AuthToken", resultado.Token);

                        var tokenInfo = _authService.ExtraerInfoToken(resultado.Token);
                        if (tokenInfo != null)
                        {
                            HttpContext.Session.SetString("UserName", tokenInfo.UserName ?? "");
                            HttpContext.Session.SetString("UserEmail", tokenInfo.Email ?? "");
                            HttpContext.Session.SetString("UserRoles", string.Join(",", tokenInfo.Roles));
                            HttpContext.Session.SetString("UserId", tokenInfo.UserId ?? "");

                            //Creamos CLAIMS para [Authorize] 
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.NameIdentifier, tokenInfo.UserId ?? ""),
                                new Claim(ClaimTypes.Name, tokenInfo.UserName ?? ""),
                                new Claim(ClaimTypes.Email, tokenInfo.Email ?? ""),
                                new Claim("AuthToken", resultado.Token) // ← Guardamos el token en la cookie también
                            };

                            // Agregar roles como claims
                            foreach (var role in tokenInfo.Roles)
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role));
                            }

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = true, // ← CAMBIO PRINCIPAL: true para cookie persistente
                                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60) // Mismo tiempo que session
                            };

                            //Hacer SIGN IN con Cookie Authentication
                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);
                        }
                    }

                    TempData["SuccessMessage"] = resultado.Message;
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", resultado.Message);
                    foreach (var error in resultado.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login");
                ModelState.AddModelError("", "Error de conexión. Intenta más tarde.");
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult Registro()
        {
            // Si ya está logueado, redirigmos a home
            if (_authService.IsAuthenticated())
            {
                TempData["InfoMessage"] = "Ya tienes una sesión activa.";
                return RedirectToAction("Index", "Home");
            }
            return View(new RegistroDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var resultado = await _authService.RegistrarAsync(model);

                if (resultado.IsSuccess)
                {
                    TempData["SuccessMessage"] = resultado.Message;
                    return RedirectToAction("Login");
                }
                else
                {
                    ModelState.AddModelError("", resultado.Message);
                    foreach (var error in resultado.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en registro");
                ModelState.AddModelError("", "Error de conexión. Intenta más tarde.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            // Si ya está logueado, redirigmos a home
            if (_authService.IsAuthenticated())
            {
                TempData["InfoMessage"] = "Ya tienes una sesión activa.";
                return RedirectToAction("Index", "Home");
            }

            return View(new ForgotPasswordDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (_authService.IsAuthenticated())
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var resultado = await _authService.ForgotPasswordAsync(model);

                if (resultado.IsSuccess)
                {
                    TempData["SuccessMessage"] = resultado.Message;
                    return RedirectToAction("ForgotPasswordConfirmation");
                }
                else
                {
                    ModelState.AddModelError("", resultado.Message);
                    foreach (var error in resultado.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en forgot password");
                ModelState.AddModelError("", "Error de conexión. Intenta más tarde.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            if (_authService.IsAuthenticated())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Enlace de restablecimiento inválido.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordDto
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validamos de que ambas contraseñas coincidan
            if (model.NuevaPassword != model.ConfirmarPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View(model);
            }

            try
            {
                var resultado = await _authService.ResetPasswordAsync(model);

                if (resultado.IsSuccess)
                {
                    TempData["SuccessMessage"] = resultado.Message;
                    return RedirectToAction("ResetPasswordConfirmation");
                }
                else
                {
                    ModelState.AddModelError("", resultado.Message);
                    foreach (var error in resultado.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en reset password");
                ModelState.AddModelError("", "Error de conexión. Intenta más tarde.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        public async Task<IActionResult> Salir()
        {
            // Limpiar Cookie Authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Limpiar Session
            _authService.Logout();

            TempData["SuccessMessage"] = "Has cerrado sesión exitosamente.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
    }
}