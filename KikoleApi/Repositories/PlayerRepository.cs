using System.Threading.Tasks;
using KikoleApi.Domain.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Repositories
{
    public class PlayerRepository : BaseRepository
    {
        public PlayerRepository(IConfiguration configuration)
            : base(configuration)
        { }

        internal async Task<long> CreatePlayerAsync(PlayerDto player)
        {
            await ExecuteInsertAsync(
                    "players",
                    new[] { "name", "year_of_birth", "country1_id", "country2_id", "proposal_date", "creation_date" },
                    new object[] { player.Name, player.YearOfBirth, player.Country1Id, player.Country2Id, player.ProposalDate, System.DateTime.Now })
                .ConfigureAwait(false);

            return await GetLastInsertedIdAsync().ConfigureAwait(false);
        }

        internal async Task CreatePlayerNamesAsync(PlayerNameDto playerName)
        {
            await ExecuteInsertAsync(
                    "player_names",
                    new[] { "player_id", "name" },
                    new object[] { playerName.PlayerId, playerName.Name })
                .ConfigureAwait(false);
        }

        internal async Task CreatePlayerClubsAsync(PlayerClubDto playerClub)
        {
            await ExecuteInsertAsync(
                    "player_clubs",
                    new[] { "player_id", "name", "history_position", "importance_position", "allowed_names" },
                    new object[] { playerClub.PlayerId, playerClub.Name, playerClub.HistoryPosition, playerClub.ImportancePosition, playerClub.AllowedNames })
                .ConfigureAwait(false);
        }
    }
}
