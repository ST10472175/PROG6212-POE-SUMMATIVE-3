using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Mvc.Controllers
{
    public sealed class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.IsInRole("Organiser"))
            {
                return RedirectToAction("Index", "Organiser");
            }

            if (User.IsInRole("Participant"))
            {
                return RedirectToAction("Index", "Participant");
            }

            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        [AllowAnonymous]
        public IActionResult Error() => View();
    }
}
