using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Melody.MVC.Controllers
{
    public class PlaylistsController : Controller
    {
        // GET: PlaylistsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: PlaylistsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PlaylistsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PlaylistsController/Create
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

        // GET: PlaylistsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PlaylistsController/Edit/5
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

        // GET: PlaylistsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PlaylistsController/Delete/5
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
