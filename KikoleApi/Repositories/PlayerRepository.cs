using System;
using System.Collections.Generic;
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
            return await ExecuteInsertAsync(
                    "players",
                    ("name", player.Name),
                    ("allowed_names", player.AllowedNames),
                    ("year_of_birth", player.YearOfBirth),
                    ("country_id", player.CountryId),
                    ("proposal_date", player.ProposalDate),
                    ("creation_date", Clock.Now),
                    ("clue", player.Clue),
                    ("position_id", player.PositionId))
                .ConfigureAwait(false);
        }

        public async Task CreatePlayerClubsAsync(PlayerClubDto playerClub)
        {
            await ExecuteInsertAsync(
                    "player_clubs",
                    ("player_id", playerClub.PlayerId),
                    ("club_id", playerClub.ClubId),
                    ("history_position", playerClub.HistoryPosition))
                .ConfigureAwait(false);
        }

        public async Task<PlayerDto> GetPlayerOfTheDayAsync(DateTime date)
        {
            return await GetDtoAsync<PlayerDto>(
                    "players",
                    ("proposal_date", date.Date))
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<PlayerClubDto>> GetPlayerClubsAsync(ulong playerId)
        {
            return await GetDtosAsync<PlayerClubDto>(
                    "player_clubs",
                    ("player_id", playerId))
                .ConfigureAwait(false);
        }

        public async Task<DateTime> GetLatestProposalDateasync()
        {
            return await ExecuteScalarAsync<DateTime>(
                    "SELECT MAX(proposal_date) FROM players", null)
                .ConfigureAwait(false);
        }
    }
}
