using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos;
using Melody.Modelos.PayPal;
using Melody.API.Consumer;
using System.Threading.Tasks;
using Melody.MVC.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Melody.MVC.Controllers
{
    public class PlanesController : Controller
    {

        private readonly AuthService _authService;

        public PlanesController(AuthService authService)
        {
            _authService = authService;
        }
        // GET: PlanesController
        public ActionResult Index()
        {
            var data = Crud<Plan>.GetAll();
            return View(data);
        }


        // GET: PlanesController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<Plan>.GetById(id);
            return View(data);
        }

        // GET: PlanesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PlanesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Plan data)
        {
            try
            {
                var token = _authService.ObtenerToken();
                await Crud<Plan>.CreateWithAuth(data, token);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear el plan";
                ModelState.AddModelError("", ex.Message);
                return View(data);
            }
        }

        // GET: PlanesController/Edit/5
        public ActionResult Edit(int id)
        {
            var data = Crud<Plan>.GetById(id);
            return View(data);
        }

        // POST: PlanesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, Plan data)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<Plan>.Update(id, data, token);

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

        // GET: PlanesController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<Plan>.GetById(id);
            return View(data);
        }

        // POST: PlanesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, Plan data)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resutado = await Crud<Plan>.DeleteWithAuth(id, token);
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