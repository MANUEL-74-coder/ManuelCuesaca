using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos;
using Melody.Modelos.PayPal;
using Melody.API.Consumer;
using System.Threading.Tasks;
using Melody.MVC.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Numerics;

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
        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                var planes = Crud<Plan>.GetAll();
                ViewBag.CurrentUser = _authService.GetCurrentUser();
                return View(planes ?? new List<Plan>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar los planes";
                return View(new List<Plan>());
            }
        }

        // GET: PlanesController/Create
        [Authorize(Roles ="admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: PlanesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="admin")]
        public async Task<ActionResult>Create(Plan plan )
        {
            try
            {
                var token = _authService.ObtenerToken();
                await Crud<Plan>.CreateWithAuth(plan, token);
                TempData["Success"] = "Plan creado exitosamente";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear el plan";
                ModelState.AddModelError("", ex.Message);
                return View(plan);
            }
        }

        // GET: PlanesController/Edit/5
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();

                var plan = await Crud<Plan>.GetByIdWithAuth<Plan>(id, token);
                if (plan == null)
                {
                    TempData["Error"] = "Plan no encontrado";
                    return RedirectToAction("Index");
                }
                return View(plan);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el plan";
                return RedirectToAction("Index");
            }
        }

        // POST: PlanesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="admin")]
        public async Task< ActionResult>Edit(int id, Plan plan)
        {
            try
            {
                var token = _authService.ObtenerToken();
                var resultado = await Crud<Plan>.Update(id, plan, token);
                if (resultado)
                {
                    TempData["Success"] = "Plan actualizado exitosamente";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "No se pudo actualizar el plan";
                    return View(plan);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar el plan";
                ModelState.AddModelError("", ex.Message);
                return View(plan);
            }
        }

        // GET: PlanesController/Delete/5
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var token = _authService.ObtenerToken();

                var data = await Crud<Plan>.GetByIdWithAuth<Plan>(id, token);
                if (data == null) return NotFound();

                return View(data);
            }
            catch
            {
                return NotFound();
            }
        }

        // POST: PlanesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="admin")]
        public async Task<ActionResult> Delete(int id, Plan plan )
        {
            try
            {
                var token = _authService.ObtenerToken();
                await Crud<Plan>.DeleteWithAuth(id, token);
                TempData["Success"] = "Plan eliminado exitosamente";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar el plan";
                return View(plan);
            }
        }
    }
}
