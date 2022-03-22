using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IPlayerRepository
    {
        Task<ulong> CreatePlayerAsync(PlayerDto player);

        Task CreatePlayerClubsAsync(PlayerClubDto playerClub);

        Task<PlayerDto> GetPlayerOfTheDayAsync(DateTime date);

        Task<PlayerDto> GetPlayerByIdAsync(ulong id);

        Task<IReadOnlyList<PlayerClubDto>> GetPlayerClubsAsync(ulong playerId);

        Task<DateTime> GetLatestProposalDateAsync();

        Task<IReadOnlyCollection<PlayerDto>> GetProposedPlayersAsync();

        Task<IReadOnlyCollection<string>> GetKnownPlayerNamesAsync(ulong userId);

        Task ValidatePlayerProposalAsync(ulong playerId, string clue, DateTime date);

        Task<IReadOnlyCollection<PlayerDto>> GetPendingValidationPlayersAsync();

        Task RefusePlayerProposalAsync(ulong playerId);
    }
}
