using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Interfaces.Repositories
{
    public interface IPlayerRepository
    {
        Task<ulong> CreatePlayerAsync(PlayerDto player);

        Task CreatePlayerClubsAsync(PlayerClubDto playerClub);

        Task<PlayerDto> GetPlayerOfTheDayAsync(DateTime date);

        Task<IReadOnlyCollection<PlayerDto>> GetPlayersOfTheDayAsync(DateTime? minimalDate, DateTime? maximalDate);

        Task<PlayerDto> GetPlayerByIdAsync(ulong id);

        Task<IReadOnlyList<PlayerClubDto>> GetPlayerClubsAsync(ulong playerId);

        Task<DateTime> GetLatestProposalDateAsync();

        Task<IReadOnlyCollection<string>> GetKnownPlayerNamesAsync(ulong userId);

        Task UpdatePlayerCluesAsync(ulong playerId, string clueEn, string easyClueEn);

        Task ValidatePlayerProposalAsync(ulong playerId, DateTime date);

        Task ChangePlayerProposalDateAsync(ulong playerId, DateTime date);

        Task InsertPlayerCluesByLanguageAsync(ulong playerId, byte isEasy, IReadOnlyDictionary<ulong, string> cluesByLanguage);

        Task<IReadOnlyCollection<PlayerDto>> GetPendingValidationPlayersAsync();

        Task RefusePlayerProposalAsync(ulong playerId);

        Task<string> GetClueAsync(ulong playerId, byte isEasy, ulong languageId);

        Task<IReadOnlyCollection<PlayerDto>> GetPlayersByCreatorAsync(ulong userId, bool? accepted);

        Task<DateTime> GetFirstDateAsync();
    }
}
