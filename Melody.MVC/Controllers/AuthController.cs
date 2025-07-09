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

        public IActionResult Salir()
        {
            _authService.Logout();
            TempData["SuccessMessage"] = "Has cerrado sesión exitosamente.";
            return RedirectToAction("Login");
        }
    }
}