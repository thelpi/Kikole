using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class HowToPlayController : KikoleBaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
