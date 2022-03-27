using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class SiteController : KikoleBaseController
    {
        private readonly IInternationalRepository _internationalRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IClock _clock;

        public SiteController(IInternationalRepository internationalRepository,
            IMessageRepository messageRepository,
            IClock clock)
        {
            _internationalRepository = internationalRepository;
            _messageRepository = messageRepository;
            _clock = clock;
        }

        [HttpGet("countries")]
        [AuthenticationLevel]
        [ProducesResponseType(typeof(IReadOnlyCollection<Country>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<Country>>> GetCountriesAsync([FromQuery] ulong languageId)
        {
            if (!Enum.GetValues(typeof(Languages)).Cast<Languages>().Select(_ => (ulong)_).Contains(languageId))
                return BadRequest("Invalid language");

            var countries = await _internationalRepository
                .GetCountriesAsync(languageId)
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
