using Melody.API.Consumer;
using Melody.Modelos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Melody.MVC.Services;

namespace Melody.MVC.Controllers
{
    [Authorize(Roles ="admin")]
    public class GenerosController : Controller
    {
        private readonly AuthService _authService;

        public GenerosController(AuthService authService)
        {
            _authService = authService;
        }
        // GET: GenerosController
        public async Task<ActionResult> Index()
        {
            try
            {
                var token = _authService.ObtenerToken();
                var data = await Crud<Genero>.GetAllWithAuth<Genero>(token);
                return View(data ?? new List<Genero>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar géneros";
                return View(new List<Genero>());
            }
        }

        // GET: GenerosController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();

                var data = await Crud<Genero>.GetByIdWithAuth<Genero>(id, token);
                if (data == null) return NotFound();

                return View(data);
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: GenerosController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: GenerosController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var data = await Crud<Genero>.GetByIdWithAuth<Genero>(id, token);
                if (data == null) return NotFound();

                return View(data);
            }
            catch
            {
                return NotFound();
            }
        }

        // POST: GenerosController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var data = await Crud<Genero>.GetByIdWithAuth<Genero>(id, token);
                if (data == null) return NotFound();

                return View(data);
            }
            catch
            {
                return NotFound();
            }
        }

        // POST: GenerosController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, Genero data)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resutado =await Crud<Genero>.DeleteWithAuth(id, token);
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
