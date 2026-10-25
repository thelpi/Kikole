using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace KikoleSite.Repositories;

public class ClubRepository : BaseRepository, IClubRepository
{
    public ClubRepository(IConfiguration configuration, IClock clock)
        : base(configuration, clock)
    { }

    public async Task<ulong> CreateClubAsync(ClubDto club)
    {
        return await ExecuteInsertAsync(
                "clubs",
                ("name", club.Name),
                ("country_id", club.CountryId),
                ("creation_date", Clock.Now));
    }

    public async Task UpdateClubAsync(ClubDto club)
    {
        await ExecuteNonQueryAsync(
                "UPDATE clubs " +
                "SET name = @name, country_id = @country_id " +
                "WHERE id = @id",
                new
                {
                    name = club.Name,
                    country_id = club.CountryId,
                    id = club.Id
                });
    }

    public async Task<ClubDto?> GetClubAsync(ulong clubId)
    {
        return await GetDtoAsync<ClubDto>(
                "clubs",
                ("id", clubId));
    }

    public async Task<IReadOnlyCollection<ClubDto>> GetClubsByIdsAsync(IReadOnlyCollection<ulong> clubIds)
    {
        if (clubIds.Count == 0)
            return [];

        return await ExecuteReaderAsync<ClubDto>(
                "SELECT * FROM clubs WHERE id IN @clubIds",
                new { clubIds });
    }

    public async Task<IReadOnlyCollection<ClubDto>> GetClubsAsync()
    {
        return await GetDtosAsync<ClubDto>(
                "clubs");
    }

    public async Task<IReadOnlyCollection<ClubTranslationDto>> GetClubTranslationsAsync()
    {
        return await GetDtosAsync<ClubTranslationDto>(
                "club_translations");
    }

    public async Task<IReadOnlyCollection<string>> GetPlayerNamesUsingClubAsync(ulong clubId)
    {
        return await ExecuteReaderAsync<string>(
                "SELECT DISTINCT p.name FROM players AS p " +
                "JOIN player_clubs AS pc ON pc.player_id = p.id " +
                "WHERE pc.club_id = @clubId " +
                "ORDER BY p.name LIMIT 20",
                new { clubId });
    }

    public async Task<ClubUsage> GetClubUsageAsync(ulong clubId)
    {
        var parameters = new { type = (ulong)ProposalTypes.Club, value = clubId.ToString() };

        var incorrect = await ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM proposals " +
                "WHERE proposal_type_id = @type AND value = @value AND successful = 0",
                parameters);

        var correct = await ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM proposals " +
                "WHERE proposal_type_id = @type AND value = @value AND successful > 0",
                parameters);

        return new ClubUsage(await GetPlayerNamesUsingClubAsync(clubId), incorrect, correct);
    }

    public async Task<ClubDeletionResult> DeleteClubAsync(ulong clubId)
    {
        try
        {
            return await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                var name = await connection.ExecuteScalarAsync<string?>(
                    "SELECT name FROM clubs WHERE id = @clubId", new { clubId }, transaction);
                if (name == null)
                    return new ClubDeletionResult(ClubDeletionOutcomes.NotFound);

                var playersCount = await connection.ExecuteScalarAsync<long>(
                    "SELECT COUNT(*) FROM player_clubs WHERE club_id = @clubId", new { clubId }, transaction);
                if (playersCount > 0)
                    return new ClubDeletionResult(ClubDeletionOutcomes.UsedByPlayers);

                var proposal = new { type = (ulong)ProposalTypes.Club, value = clubId.ToString(), name };
                var correctCount = await connection.ExecuteScalarAsync<long>(
                    "SELECT COUNT(*) FROM proposals " +
                    "WHERE proposal_type_id = @type AND value = @value AND successful > 0",
                    proposal, transaction);
                if (correctCount > 0)
                    return new ClubDeletionResult(ClubDeletionOutcomes.UsedByCorrectProposals);

                var converted = await connection.ExecuteAsync(
                    "UPDATE proposals SET value = @name " +
                    "WHERE proposal_type_id = @type AND value = @value",
                    proposal, transaction);

                await connection.ExecuteAsync(
                    "DELETE FROM club_translations WHERE club_id = @clubId", new { clubId }, transaction);
                await connection.ExecuteAsync(
                    "DELETE FROM clubs WHERE id = @clubId", new { clubId }, transaction);

                return new ClubDeletionResult(ClubDeletionOutcomes.Deleted, converted);
            });
        }
        catch (MySqlException ex) when (ex.ErrorCode is MySqlErrorCode.RowIsReferenced or MySqlErrorCode.RowIsReferenced2)
        {
            // un kikole a ajoute ce club entre la verification et la suppression
            return new ClubDeletionResult(ClubDeletionOutcomes.UsedByPlayers);
        }
    }

    public async Task ReplaceClubTranslationsAsync(ulong clubId, IReadOnlyCollection<ClubTranslationDto> translations)
    {
        await ExecuteNonQueryAsync(
                "DELETE FROM club_translations WHERE club_id = @clubId",
                new { clubId });

        foreach (var translation in translations)
        {
            await ExecuteInsertAsync(
                    "club_translations",
                    ("club_id", clubId),
                    ("language_id", translation.LanguageId),
                    ("priority", translation.Priority),
                    ("name", translation.Name));
        }
    }
}
