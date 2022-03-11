using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public abstract class KikoleBaseController : Controller
    {
        protected string GetSubmitAction()
        {
            var submitKeys = HttpContext.Request.Form.Keys.Where(x => x.StartsWith("submit-"));

            if (submitKeys.Count() != 1)
                return null;

            var submitKeySplit = submitKeys.First().Split('-');
            if (submitKeySplit.Length != 2)
                return null;

            return submitKeySplit[1];
        }
    }
}
