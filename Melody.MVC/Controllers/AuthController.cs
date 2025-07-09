using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;

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
            return View(new LoginDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginDto model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var resultado = _authService.Login(model);

                if (resultado.IsSuccess)
                {
                    if (!string.IsNullOrEmpty(resultado.Token))
                    {
                        HttpContext.Session.SetString("AuthToken", resultado.Token);

                        var tokenInfo = _authService.ExtraerInfoToken(resultado.Token);
                        if (tokenInfo != null)
                        {
                            HttpContext.Session.SetString("UserName", tokenInfo.UserName ?? "");
                            HttpContext.Session.SetString("UserEmail", tokenInfo.Email ?? "");
                            HttpContext.Session.SetString("UserRoles", string.Join(",", tokenInfo.Roles));
                            HttpContext.Session.SetString("UserId", tokenInfo.UserId ?? "");
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
            return View(new RegistroDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registro(RegistroDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var resultado = _authService.Registrar(model);

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
            // Si ya está logueado, redirigir
            if (_authService.IsAuthenticated())
            {
                TempData["InfoMessage"] = "Ya tienes una sesión activa.";
                return RedirectToAction("Index", "Home");
            }

            return View(new ForgotPasswordDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordDto model)
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
                var resultado = _authService.ForgotPassword(model);

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
        public IActionResult ResetPassword(ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validación de que ambas contraseñas coincidan
            if (model.NuevaPassword != model.ConfirmarPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View(model);
            }

            try
            {
                var resultado = _authService.ResetPassword(model);

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


        public IActionResult Salir()
        {
            _authService.Logout();
            TempData["SuccessMessage"] = "Has cerrado sesión exitosamente.";
            return RedirectToAction("Login");
        }
    }
}