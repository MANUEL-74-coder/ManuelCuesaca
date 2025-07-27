using Melody.API.Consumer;
using Melody.Modelos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Melody.MVC.Services;

namespace Melody.MVC.Controllers
{
    public class GenerosController : Controller
    {
        private readonly AuthService _authService;

        public GenerosController(AuthService authService)
        {
            _authService = authService;
        }
        // GET: GenerosController
        [AllowAnonymous]
        public ActionResult Index()
        {
            var data = Crud<Genero>.GetAll();
            return View(data);
        }

        // GET: GenerosController/Details/5
        [Authorize]
        public ActionResult Details(int id)
        {
            var data = Crud<Genero>.GetById(id);
            return View(data);
        }

        // GET: GenerosController/Create
        [Authorize(Roles = "admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: GenerosController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Create(Genero data)
        {
            try
            {
                var token = _authService.ObtenerToken();
                await Crud<Genero>.CreateWithAuth(data, token);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear el género";
                ModelState.AddModelError("", ex.Message);
                return View(data);
            }
        }

        // GET: GenerosController/Edit/5
        [Authorize(Roles = "admin")]
        public ActionResult Edit(int id)
        {
            var token = _authService.ObtenerToken();
            var data = Crud<Genero>.GetById(id);
            return View(data);
        }

        // POST: GenerosController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Edit(int id, Genero data)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<Genero>.Update(id, data, token);

                if (resultado)
                {
                    TempData["Success"] = "Género actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "No se pudo actualizar el género";
                    return View(data);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar sus canciones";
                ModelState.AddModelError("", ex.Message);
                return View(data);
            }
        }

        // GET: GenerosController/Delete/5
        [Authorize(Roles = "admin")]
        public ActionResult Delete(int id)
        {
            var data = Crud<Genero>.GetById(id);
            return View(data);
        }

        // POST: GenerosController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Delete(int id, Genero data)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resutado = await Crud<Genero>.DeleteWithAuth(id, token);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(data);
            }
        }
    }
}