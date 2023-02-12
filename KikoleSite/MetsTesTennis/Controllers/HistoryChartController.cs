using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.MetsTesTennis.Enums;
using KikoleSite.MetsTesTennis.Models;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.MetsTesTennis.Controllers
{
    [Route("mets-tes-tennis")]
    public class HistoryChartController : Controller
    {
        private const int EraOpenFirstYear = 1968;
        private const int AtpTourFirstYear = 1990;

        private readonly SqlRepository _repository;
        private readonly IClock _clock;

        public HistoryChartController(SqlRepository repository, IClock clock)
        {
            _repository = repository;
            _clock = clock;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int startYear = AtpTourFirstYear)
        {
            startYear = EnsureYearParameter(startYear, _clock);

            var years = new List<YearChartItemData>(100);

            var slotsDto = await _repository
                .GetSlotsAsync()
                .ConfigureAwait(false);

            var slots = slotsDto
                .OrderBy(s => s.Position)
                .Select(ToSlotHeaderItemData)
                .ToList();

            for (var year = _clock.Today.Year; year >= startYear; year--)
            {
                var editions = new List<EditionChartItemData>(100);

                var editionsDto = await _repository
                    .GetEditionsByYearAsync(year)
                    .ConfigureAwait(false);

                foreach (var editionDto in editionsDto.Where(e => e.SlotId.HasValue))
                {
                    var winnerDto = await _repository
                        .GetEditionWinnerAsync(editionDto.Id)
                        .ConfigureAwait(false);

                    editions.Add(ToEditionChartItemData(editionDto, winnerDto));
                }

                years.Add(new YearChartItemData
                {
                    Year = year,
                    EditionCharts = editions
                });
            }

            RemoveSlotsWithoutTournament(years, slots);

            foreach (var slot in slots)
            {
                SetSlotMainTournament(years, slot);
            }

            var model = new HistoryChartViewData
            {
                SlotHeaders = slots,
                YearCharts = years
            };

            return View($"MetsTesTennis/Views/HistoryChart/Index.cshtml", model);
        }

        private static void SetSlotMainTournament(List<YearChartItemData> years, SlotHeaderItemData slot)
        {
            slot.Tournaments = years
                .SelectMany(y => y.EditionCharts)
                .Where(ec => ec.SlotId == slot.SlotId)
                .GroupBy(ec => ec.TournamentId)
                .Select(x => (x.Key, x.First().TournamentName))
                .ToList();
        }

        private static void RemoveSlotsWithoutTournament(List<YearChartItemData> years, List<SlotHeaderItemData> slots)
        {
            slots.RemoveAll(s =>
                !years.SelectMany(y => y.EditionCharts).Any(ec => ec.SlotId == s.SlotId));
        }

        private static EditionChartItemData ToEditionChartItemData(Dtos.EditionDto editionDto, Dtos.PlayerDto winnerDto)
        {
            var edition = new EditionChartItemData
            {
                SlotId = editionDto.SlotId.Value,
                Surface = editionDto.SurfaceId.HasValue
                    ? (Surfaces)editionDto.SurfaceId
                    : default(Surfaces?),
                Indoor = editionDto.Indoor != 0,
                TournamentName = editionDto.Name,
                TournamentId = editionDto.TournamentId,
            };

            if (winnerDto != null)
            {
                edition.WinnerId = winnerDto.Id;
                edition.WinnerName = string.Concat(winnerDto.LastName, ", ", winnerDto.FirstName);
            }

            return edition;
        }

        private static SlotHeaderItemData ToSlotHeaderItemData(Dtos.SlotDto s)
        {
            return new SlotHeaderItemData
            {
                SlotId = s.Id,
                SlotLevel = (Levels)s.LevelId
            };
        }

        private static int EnsureYearParameter(int startYear, IClock clock)
        {
            return Math.Min(Math.Max(startYear, EraOpenFirstYear), clock.Today.Year);
        }
    }
}
