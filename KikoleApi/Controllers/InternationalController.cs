using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KikoleApi.Controllers.Filters;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleApi.Controllers
{
    public class InternationalController : KikoleBaseController
    {
        private readonly IInternationalRepository _internationalRepository;

        public InternationalController(IInternationalRepository internationalRepository)
        {
            _internationalRepository = internationalRepository;
        }

        [HttpGet("countries")]
        [AuthenticationLevel(AuthenticationLevel.None)]
        [ProducesResponseType(typeof(IReadOnlyCollection<CountryModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<CountryModel>>> GetCountriesAsync([FromQuery] ulong languageId)
        {
            if (!Enum.GetValues(typeof(Language)).Cast<Language>().Select(_ => (ulong)_).Contains(languageId))
                return BadRequest("Invalid language");

            var countries = await _internationalRepository
                .GetCountriesAsync(languageId)
                .ConfigureAwait(false);

            return Ok(countries.Select(c => new CountryModel(c)).ToList());
        }
    }
}
