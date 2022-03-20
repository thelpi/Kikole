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
    }
}
