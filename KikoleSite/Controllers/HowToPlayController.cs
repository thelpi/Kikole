using KikoleSite.Api;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers
{
    public class HowToPlayController : KikoleBaseController
    {
        private readonly IApiProvider _apiProvider;

        private static ProposalChart _proposalChartCache;

        public HowToPlayController(IApiProvider apiProvider)
        {
            _apiProvider = apiProvider;
        }

        public IActionResult Index()
        {
            return View(GetProposalChartCache());
        }

        private ProposalChart GetProposalChartCache()
        {
            return _proposalChartCache ??
                (_proposalChartCache = _apiProvider.GetProposalChartAsync().GetAwaiter().GetResult());
        }
    }
}
