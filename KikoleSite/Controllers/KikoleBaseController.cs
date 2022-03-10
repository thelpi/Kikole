using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public abstract class KikoleBaseController : Controller
    {
        protected string GetSubmitAction()
        {
            return HttpContext.Request.Form.Keys.Single(x => x.StartsWith("submit-")).Split('-')[1];
        }
    }
}
