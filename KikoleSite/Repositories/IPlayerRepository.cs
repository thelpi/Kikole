using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface IPlayerRepository
{
    Task<ulong> CreatePlayerAsync(PlayerDto player);

    Task CreatePlayerClubsAsync(PlayerClubDto playerClub);

    Task<PlayerDto?> GetPlayerOfTheDayAsync(DateOnly date);

    Task<IReadOnlyCollection<PlayerDto>> GetPlayersOfTheDayAsync(DateOnly? minimalDate, DateOnly? maximalDate);

    Task<PlayerDto?> GetPlayerByIdAsync(ulong id);

    Task<IReadOnlyList<PlayerClubDto>> GetPlayerClubsAsync(ulong playerId);

    Task<DateOnly> GetLatestPlayerDateAsync();

    Task<DateOnly?> GetEarliestPlayerDateAsync();

    Task UpdatePlayerCluesAsync(ulong playerId, string clueEn, string easyClueEn);

    Task ValidatePlayerProposalAsync(ulong playerId, DateOnly date);

    Task ChangePlayerPublicationDateAsync(ulong playerId, DateOnly date);

    Task InsertPlayerCluesByLanguageAsync(ulong playerId, byte isEasy, IReadOnlyDictionary<ulong, string> cluesByLanguage);

    Task<IReadOnlyCollection<PlayerDto>> GetPendingValidationPlayersAsync();

    Task RefusePlayerProposalAsync(ulong playerId);

    Task<string?> GetClueAsync(ulong playerId, byte isEasy, ulong languageId);

    Task<IReadOnlyCollection<PlayerDto>> GetPlayersByCreatorAsync(ulong userId, bool? accepted);

    Task<bool> UpdatePlayerAsync(
        ulong playerId,
        PlayerDto player,
        IReadOnlyList<PlayerClubDto> clubs,
        IReadOnlyDictionary<ulong, string> clues,
        IReadOnlyDictionary<ulong, string> easyClues);
}
