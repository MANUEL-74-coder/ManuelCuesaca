using Melody.API.Consumer;
using Melody.Modelos;
using Melody.Modelos.DTOs;
using Melody.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Melody.MVC.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly AuthService _authService;

        public UsuariosController(AuthService authService)
        {
            _authService = authService;
        }
        // GET: Mi perfil
        public async Task<IActionResult> MiPerfil()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var perfil = await Crud<MiPerfilDto>.GetWithAuth("mi-perfil", token);
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(perfil);
            }
            catch
            {
                TempData["Error"] = "Error al cargar el perfil";
                return RedirectToAction("Index", "Home");
            }
        }
        // GET: UsuariosController/Details/5
        public async Task<ActionResult> Index(int id)
        {
            var token = _authService.ObtenerToken();
            var data = await Crud<Usuario>.GetAllWithAuth<Usuario>(token);
            return View(data);
        }


        // GET: UsuariosController/Details/5
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Details(int id)
        {
            var token = _authService.ObtenerToken();
            var data = await Crud<Usuario>.GetByIdWithAuth<Usuario>(id, token);

            return View(data);
        }


        // GET: UsuariosController/Edit/5
        public async Task<ActionResult> EditarPerfil()
        {
            ViewBag.CurrentUser = _authService.GetCurrentUser();

            try
            {
                var token = _authService.ObtenerToken();
                var perfilActual = await Crud<MiPerfilDto>.GetWithAuth("mi-perfil", token);
                var model = new ActualizarPerfilUsuarioDto
                {
                    Nombre = perfilActual.Nombre,
                    Apellido = perfilActual.Apellido,
                };
                ViewBag.ImagenPerfilActual = perfilActual.FotoPerfil;

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el perfil del artista";
                return RedirectToAction("MiPerfil");
            }
        }

        // POST: UsuariosController/Edit/5
        // POST: Actualizar perfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(ActualizarPerfilUsuarioDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(model);
            }

            try
            {
                var token = _authService.ObtenerToken();

                using var formData = new MultipartFormDataContent();

                if (!string.IsNullOrEmpty(model.Nombre))
                    formData.Add(new StringContent(model.Nombre), "Nombre");

                if (!string.IsNullOrEmpty(model.Apellido))
                    formData.Add(new StringContent(model.Apellido), "Apellido");

                if (model.FotoPerfil != null)
                {
                    var fotoContent = new StreamContent(model.FotoPerfil.OpenReadStream());
                    fotoContent.Headers.ContentType = new MediaTypeHeaderValue(model.FotoPerfil.ContentType);
                    formData.Add(fotoContent, "FotoPerfil", model.FotoPerfil.FileName);
                }

                var resultado = await Crud<MiPerfilDto>.UpdateWithFormData("mi-perfil", formData, token);


                if (resultado)
                {
                    TempData["Success"] = "Perfil actualizado exitosamente";
                    return RedirectToAction(nameof(MiPerfil));
                }
                else
                {
                    TempData["Error"] = "Error al actualizar el perfil";
                }
            }
            catch
            {
                TempData["Error"] = "Error al actualizar el perfil";
            }

            ViewBag.CurrentUser = _authService.GetCurrentUser();
            return View(model);
        }

    }
}