using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;
using Melody.API.Consumer;
using Melody.Modelos;

namespace Melody.MVC.Controllers
{
    [Authorize(Roles = "userfree,userpremium")]
    public class MeGustaController : Controller
    {
        private readonly AuthService _authService;

        public MeGustaController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: MeGusta - Mis canciones favoritas
        public async Task<IActionResult> Index()
        {
            try
            {
                var token = _authService.ObtenerToken();

                var favoritos = await Crud<MeGustaDto>.GetListWithAuth("mis-favoritos", token);

                ViewBag.CurrentUser = _authService.GetCurrentUser();
                ViewBag.TotalFavoritos = favoritos?.Count ?? 0;

                return View(favoritos ?? new List<MeGustaDto>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar tus canciones favoritas";
                return View(new List<MeGustaDto>());
            }
        }
    }
}