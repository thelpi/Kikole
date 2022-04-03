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
                    ("position_id", player.PositionId),
                    ("creation_user_id", player.CreationUserId),
                    ("hide_creator", player.HideCreator))
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

        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersOfTheDayAsync(
            DateTime? minimalDate, DateTime? maximalDate)
        {
            return await ExecuteReaderAsync<PlayerDto>(
                    "SELECT * FROM players " +
                    "WHERE proposal_date IS NOT NULL " +
                    "AND (@min_date IS NULL OR proposal_date >= @min_date) " +
                    "AND (proposal_date <= IFNULL(@max_date, DATE(NOW())))",
                    new
                    {
                        min_date = minimalDate?.Date,
                        max_date = maximalDate?.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<PlayerClubDto>> GetPlayerClubsAsync(ulong playerId)
        {
            return await GetDtosAsync<PlayerClubDto>(
                    "player_clubs",
                    ("player_id", playerId))
                .ConfigureAwait(false);
        }

        public async Task<DateTime> GetLatestProposalDateAsync()
        {
            return await ExecuteScalarAsync<DateTime>(
                    "SELECT MAX(proposal_date) FROM players", null)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayerDto>> GetProposedPlayersAsync()
        {
            return await ExecuteReaderAsync<PlayerDto>(
                    "SELECT * FROM players WHERE proposal_date <= DATE(NOW())", null)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<string>> GetKnownPlayerNamesAsync(ulong userId)
        {
            return await ExecuteReaderAsync<string>(
                    "SELECT y.name FROM players AS y " +
                    "LEFT JOIN proposals AS p ON y.proposal_date = DATE(DATE_ADD(p.proposal_date, INTERVAL -p.days_before DAY)) " +
                    "WHERE (p.user_id = @userId AND p.successful = 1 AND p.proposal_type_id = 1) " +
                    "OR y.creation_user_id = @userId",
                    new { userId })
                .ConfigureAwait(false);
        }

        public async Task<PlayerDto> GetPlayerByIdAsync(ulong id)
        {
            return await GetDtoAsync<PlayerDto>(
                    "players",
                    ("id", id))
                .ConfigureAwait(false);
        }

        public async Task ValidatePlayerProposalAsync(ulong playerId, string clue, DateTime date)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE players " +
                    "SET proposal_date = @date, clue = @clue " +
                    "WHERE id = @playerId",
                    new
                    {
                        playerId,
                        clue,
                        date.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayerDto>> GetPendingValidationPlayersAsync()
        {
            return await ExecuteReaderAsync<PlayerDto>(
                    "SELECT * FROM players " +
                    "WHERE proposal_date IS NULL " +
                    "AND reject_date IS NULL",
                    new { })
                .ConfigureAwait(false);
        }

        public async Task RefusePlayerProposalAsync(ulong playerId)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE players SET reject_date = NOW() WHERE id = @playerId",
                    new { playerId })
                .ConfigureAwait(false);
        }

        public async Task<string> GetClueAsync(ulong playerId, ulong languageId)
        {
            return await ExecuteScalarAsync<string>(
                    "SELECT clue FROM player_clue_translations " +
                    "WHERE player_id = @playerId " +
                    "AND language_id = @languageId",
                    new { playerId, languageId })
                .ConfigureAwait(false);
        }
    }
}
