using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class HowToPlayController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
