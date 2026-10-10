using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories;

public class PlayerRepository : BaseRepository, IPlayerRepository
{
    public PlayerRepository(IConfiguration configuration, IClock clock)
        : base(configuration, clock)
    { }

    public async Task<ulong> CreatePlayerAsync(PlayerDto player)
    {
        var now = Clock.Now;

        return await ExecuteInsertAsync(
                "players",
                ("name", player.Name),
                ("allowed_names", player.AllowedNames),
                ("year_of_birth", player.YearOfBirth),
                ("country_id", player.CountryId),
                ("alternative_country_id", player.AlternativeCountryId),
                ("publication_date", player.PublicationDate),
                ("creation_date", now),
                // un kikole cree directement date (administrateur) est accepte d'office, a sa
                // creation ; une soumission en attente (pas de date) ne l'est pas encore
                ("acceptance_date", player.PublicationDate.HasValue ? now : null),
                ("clue", player.Clue),
                ("easy_clue", player.EasyClue),
                ("position_id", player.PositionId),
                ("alternative_position_id", player.AlternativePositionId),
                ("creation_user_id", player.CreationUserId),
                ("hide_creator", player.HideCreator));
    }

    public async Task CreatePlayerClubsAsync(PlayerClubDto playerClub)
    {
        await ExecuteInsertAsync(
                "player_clubs",
                ("player_id", playerClub.PlayerId),
                ("club_id", playerClub.ClubId),
                ("history_position", playerClub.HistoryPosition),
                ("is_loan", playerClub.IsLoan));
    }

    public async Task<PlayerDto?> GetPlayerOfTheDayAsync(DateOnly date)
    {
        return await GetDtoAsync<PlayerDto>(
                "players",
                ("publication_date", date));
    }

    public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersOfTheDayAsync(
        DateOnly? minimalDate, DateOnly? maximalDate)
    {
        return await ExecuteReaderAsync<PlayerDto>(
                "SELECT * FROM players " +
                "WHERE publication_date IS NOT NULL " +
                "AND (@min_date IS NULL OR publication_date >= @min_date) " +
                "AND (@max_date IS NULL OR publication_date <= @max_date)",
                new
                {
                    min_date = minimalDate,
                    max_date = maximalDate
                });
    }

    public async Task<IReadOnlyList<PlayerClubDto>> GetPlayerClubsAsync(ulong playerId)
    {
        return await GetDtosAsync<PlayerClubDto>(
                "player_clubs",
                ("player_id", playerId));
    }

    public async Task<DateOnly> GetLatestPlayerDateAsync()
    {
        return await ExecuteScalarAsync<DateOnly>(
                "SELECT MAX(publication_date) FROM players", null);
    }

    public async Task<DateOnly?> GetEarliestPlayerDateAsync()
    {
        return await ExecuteScalarAsync<DateOnly?>(
                "SELECT MIN(publication_date) FROM players", null);
    }

    public async Task<PlayerDto?> GetPlayerByIdAsync(ulong id)
    {
        return await GetDtoAsync<PlayerDto>(
                "players",
                ("id", id));
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
                });
    }

    public async Task ValidatePlayerProposalAsync(ulong playerId, DateOnly date)
    {
        await ExecuteNonQueryAsync(
                "UPDATE players " +
                "SET publication_date = @date, acceptance_date = @now " +
                "WHERE id = @playerId",
                new
                {
                    playerId,
                    date,
                    now = Clock.Now
                });
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
                    ("clue", cluesByLanguage[languageId]));
        }
    }

    public async Task<IReadOnlyCollection<PlayerDto>> GetPendingValidationPlayersAsync()
    {
        return await ExecuteReaderAsync<PlayerDto>(
                "SELECT * FROM players " +
                "WHERE publication_date IS NULL " +
                "AND reject_date IS NULL",
                new { });
    }

    public async Task RefusePlayerProposalAsync(ulong playerId)
    {
        await ExecuteNonQueryAsync(
                "UPDATE players SET reject_date = NOW() WHERE id = @playerId",
                new { playerId });
    }

    public async Task<string?> GetClueAsync(ulong playerId, byte isEasy, ulong languageId)
    {
        return await ExecuteScalarAsync<string>(
                "SELECT clue FROM player_clue_translations " +
                "WHERE player_id = @playerId " +
                "AND language_id = @languageId " +
                "AND is_easy = @isEasy",
                new { playerId, languageId, isEasy });
    }

    public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersByCreatorAsync(ulong userId, bool? accepted)
    {
        return await ExecuteReaderAsync<PlayerDto>(
                "SELECT * FROM players " +
                "WHERE creation_user_id = @userId " +
                "AND (" +
                "(@type = 1 AND publication_date IS NOT NULL) " +
                "OR (@type = 2 AND reject_date IS NOT NULL) " +
                "OR @type = 0 " +
                ")",
                new { userId, type = (accepted.HasValue ? (accepted.Value ? 1 : 2) : 0) });
    }

    public async Task<bool> UpdatePlayerAsync(
        ulong playerId,
        PlayerDto player,
        IReadOnlyList<PlayerClubDto> clubs,
        IReadOnlyDictionary<ulong, string> clues,
        IReadOnlyDictionary<ulong, string> easyClues)
    {
        return await ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            // garde atomique : seul un kikole en attente ou programme apres aujourd'hui est modifiable
            var updated = await connection.ExecuteAsync(
                "UPDATE players SET name = @Name, allowed_names = @AllowedNames, year_of_birth = @YearOfBirth, " +
                "country_id = @CountryId, alternative_country_id = @AlternativeCountryId, " +
                "clue = @Clue, easy_clue = @EasyClue, position_id = @PositionId, " +
                "alternative_position_id = @AlternativePositionId, hide_creator = @HideCreator " +
                "WHERE id = @playerId AND reject_date IS NULL " +
                "AND (publication_date IS NULL OR publication_date > @today)",
                new
                {
                    playerId,
                    today = Clock.Today,
                    player.Name,
                    player.AllowedNames,
                    player.YearOfBirth,
                    player.CountryId,
                    player.AlternativeCountryId,
                    player.Clue,
                    player.EasyClue,
                    player.PositionId,
                    player.AlternativePositionId,
                    player.HideCreator
                },
                transaction);

            if (updated == 0)
                return false;

            await connection.ExecuteAsync("DELETE FROM player_clubs WHERE player_id = @playerId", new { playerId }, transaction);
            foreach (var club in clubs)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO player_clubs (player_id, club_id, history_position, is_loan) " +
                    "VALUES (@PlayerId, @ClubId, @HistoryPosition, @IsLoan)",
                    club,
                    transaction);
            }

            await connection.ExecuteAsync("DELETE FROM player_clue_translations WHERE player_id = @playerId", new { playerId }, transaction);
            foreach (var (languageId, clue) in clues)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO player_clue_translations (player_id, language_id, is_easy, clue) VALUES (@playerId, @languageId, 0, @clue)",
                    new { playerId, languageId, clue },
                    transaction);
            }
            foreach (var (languageId, clue) in easyClues)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO player_clue_translations (player_id, language_id, is_easy, clue) VALUES (@playerId, @languageId, 1, @clue)",
                    new { playerId, languageId, clue },
                    transaction);
            }

            return true;
        });
    }

    public async Task ChangePlayerPublicationDateAsync(ulong playerId, DateOnly date)
    {
        await ExecuteNonQueryAsync(
                "UPDATE players " +
                "SET publication_date = @publicationDate " +
                "WHERE id = @playerId",
                new { playerId, publicationDate = date });
    }
}
