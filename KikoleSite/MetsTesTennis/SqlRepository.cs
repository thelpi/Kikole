using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.MetsTesTennis.Dtos;
using KikoleSite.MetsTesTennis.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.MetsTesTennis
{
    public class SqlRepository : Repositories.BaseRepository
    {
        public SqlRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<IReadOnlyList<SlotDto>> GetSlotsAsync()
        {
            return await GetDtosAsync<SlotDto>(
                    "mtt_slots")
                .ConfigureAwait(false);
        }

        public async Task<PlayerDto> GetEditionWinnerAsync(ulong editionId)
        {
            var dtos = await ExecuteReaderAsync<PlayerDto>(
                    "SELECT * FROM mtt_players WHERE id IN (" +
                    "   SELECT winner_id FROM mtt_matches WHERE round_id = @roundId AND edition_id = @editionId)",
                    new
                    {
                        editionId,
                        roundId = (byte)Rounds.F
                    })
                .ConfigureAwait(false);

            return dtos.FirstOrDefault();
        }

        public async Task<IReadOnlyList<EditionDto>> GetEditionsByYearAsync(int year)
        {
            return await GetDtosAsync<EditionDto>(
                    "mtt_editions",
                    ("year", year))
                .ConfigureAwait(false);
        }
    }
}
