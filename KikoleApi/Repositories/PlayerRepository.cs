using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    public class PlayerRepository : BaseRepository, IPlayerRepository
    {
        public PlayerRepository(IConfiguration configuration, IClock clock)
            : base(configuration, clock)
        { }

        public async Task<ulong> CreatePlayerAsync(PlayerDto player)
        {
            await ExecuteInsertAsync(
                    "players",
                    new[] { "name", "allowed_names", "year_of_birth", "country1_id", "country2_id", "proposal_date", "creation_date" },
                    new object[] { player.Name, player.AllowedNames, player.YearOfBirth, player.Country1Id, player.Country2Id, player.ProposalDate, Clock.Now })
                .ConfigureAwait(false);

            return await GetLastInsertedIdAsync().ConfigureAwait(false);
        }

        public async Task CreatePlayerClubsAsync(PlayerClubDto playerClub)
        {
            await ExecuteInsertAsync(
                    "player_clubs",
                    new[] { "player_id", "club_id", "history_position", "importance_position" },
                    new object[] { playerClub.PlayerId, playerClub.ClubId, playerClub.HistoryPosition, playerClub.ImportancePosition })
                .ConfigureAwait(false);
        }
    }
}
