using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Melody.Modelos;
using Melody.API.Consumer;
using System.Threading.Tasks;
using Melody.MVC.Services;

namespace Melody.MVC.Controllers
{
    [Authorize(Roles = "admin")]
    public class PlanesController : Controller
    {

        private readonly AuthService _authService;

        public PlanesController(AuthService authService)
        {
            _authService = authService;
        }
        // GET: PlanesController
        public async Task<ActionResult> Index()
        {
            var token = _authService.ObtenerToken();
            var data = Crud<Plan>.GetAllWithAuth<Plan>(token);
            return View();
        }

        // GET: PlanesController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var token = _authService.ObtenerToken();
            var data = Crud<Plan>.GetByIdWithAuth<Plan>(id, token);
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
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PlanesController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PlanesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PlanesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PlanesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}