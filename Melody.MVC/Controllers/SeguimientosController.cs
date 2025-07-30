using Melody.API.Consumer;
using Melody.Modelos;
using Melody.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Melody.MVC.Controllers
{
    public class SeguimientosController : Controller
    {
        private readonly AuthService _authService;

        public SeguimientosController(AuthService authService)
        {
            _authService = authService;
        }
        // GET: Mis seguimientos
        [Authorize]
        public async Task<IActionResult> MisSeguimientos()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var seguimientos = await Crud<Seguimiento>.GetListWithAuth("mis-seguimientos", token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(seguimientos);
            }
            catch
            {
                TempData["Error"] = "Error al cargar tus seguimientos";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
