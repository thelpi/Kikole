using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Api.Repositories
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
                    ("easy_clue", player.EasyClue),
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
                    ("history_position", playerClub.HistoryPosition),
                    ("is_loan", playerClub.IsLoan))
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
                    "AND (@max_date IS NULL OR proposal_date <= @max_date)",
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

        public async Task<IReadOnlyCollection<string>> GetKnownPlayerNamesAsync(ulong userId)
        {
            return await ExecuteReaderAsync<string>(
                    "SELECT DISTINCT y.name " +
                    "FROM players AS y " +
                    "LEFT JOIN proposals AS p ON y.proposal_date = p.proposal_date " +
                    "WHERE (" +
                    "   (p.user_id = @userId AND p.successful = 1 AND p.proposal_type_id = @pType) " +
                    "   OR y.creation_user_id = @userId" +
                    ") AND y.proposal_date IS NOT NULL " +
                    "AND y.proposal_date <= DATE(NOW())",
                    new { userId, pType = (ulong)Models.Enums.ProposalTypes.Name })
                .ConfigureAwait(false);
        }

        public async Task<PlayerDto> GetPlayerByIdAsync(ulong id)
        {
            return await GetDtoAsync<PlayerDto>(
                    "players",
                    ("id", id))
                .ConfigureAwait(false);
        }

        public async Task UpdatePlayerCluesAsync(ulong playerId, string clueEn, string easyClueEn)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE players " +
                    "SET clue = @clueEn, easy_clue = @easyClueEn " +
                    "WHERE id = @playerId",
                    new
                    {
                        playerId,
                        clueEn,
                        easyClueEn
                    })
                .ConfigureAwait(false);
        }

        public async Task ValidatePlayerProposalAsync(ulong playerId, DateTime date)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE players " +
                    "SET proposal_date = @date " +
                    "WHERE id = @playerId",
                    new
                    {
                        playerId,
                        date.Date
                    })
                .ConfigureAwait(false);
        }

        public async Task InsertPlayerCluesByLanguageAsync(ulong playerId, byte isEasy, IReadOnlyDictionary<ulong, string> cluesByLanguage)
        {
            foreach (var languageId in cluesByLanguage.Keys)
            {
                await ExecuteReplaceAsync(
                        "player_clue_translations",
                        ("player_id", playerId),
                        ("language_id", languageId),
                        ("is_easy", isEasy),
                        ("clue", cluesByLanguage[languageId]))
                    .ConfigureAwait(false);
            }
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

        public async Task<string> GetClueAsync(ulong playerId, byte isEasy, ulong languageId)
        {
            return await ExecuteScalarAsync<string>(
                    "SELECT clue FROM player_clue_translations " +
                    "WHERE player_id = @playerId " +
                    "AND language_id = @languageId " +
                    "AND is_easy = @isEasy",
                    new { playerId, languageId, isEasy })
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersByCreatorAsync(ulong userId, bool? accepted)
        {
            return await ExecuteReaderAsync<PlayerDto>(
                    "SELECT * FROM players " +
                    "WHERE creation_user_id = @userId " +
                    "AND (" +
                    "(@type = 1 AND proposal_date IS NOT NULL) " +
                    "OR (@type = 2 AND reject_date IS NOT NULL) " +
                    "OR @type = 0 " +
                    ")",
                    new { userId, type = (accepted.HasValue ? (accepted.Value ? 1 : 2) : 0) })
                .ConfigureAwait(false);
        }

        public async Task<DateTime> GetFirstDateAsync()
        {
            return await ExecuteScalarAsync<DateTime>(
                    "SELECT DATE_ADD(MIN(proposal_date), INTERVAL 1 DAY) " +
                    "FROM players " +
                    "WHERE proposal_date IS NOT NULL",
                    new object())
                .ConfigureAwait(false);
        }

        public async Task ChangePlayerProposalDateAsync(ulong playerId, DateTime date)
        {
            await ExecuteNonQueryAsync(
                    "UPDATE players " +
                    "SET proposal_date = @proposalDate " +
                    "WHERE id = @playerId",
                    new { playerId, proposalDate = date.Date })
                .ConfigureAwait(false);
        }
    }
}
