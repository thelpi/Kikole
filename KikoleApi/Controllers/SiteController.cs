using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Interfaces.Repositories;
using KikoleApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class SiteController : KikoleBaseController
    {
        private readonly IInternationalRepository _internationalRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IClock _clock;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SiteController(IInternationalRepository internationalRepository,
            IMessageRepository messageRepository,
            IHttpContextAccessor httpContextAccessor,
            IClock clock)
        {
            _internationalRepository = internationalRepository;
            _messageRepository = messageRepository;
            _clock = clock;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet("countries")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<Country>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Country>>> GetCountriesAsync()
        {
            var countries = await _internationalRepository
                .GetCountriesAsync((ulong)(_httpContextAccessor.ExtractLanguage()))
                .ConfigureAwait(false);

            return Ok(countries.Select(c => new Country(c)).OrderBy(c => c.Name).ToList());
        }

        [HttpGet("current-messages")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<string>> GetCurrentMessage()
        {
            var message = await _messageRepository
                .GetMessageAsync(_clock.Now)
                .ConfigureAwait(false);

            return Ok(message?.Message);
        }
    }
}
