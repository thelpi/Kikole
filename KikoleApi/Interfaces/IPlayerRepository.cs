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

        Task<IReadOnlyList<PlayerClubDto>> GetPlayerClubsAsync(ulong playerId);

        Task<DateTime> GetLatestProposalDateasync();

        Task<IReadOnlyCollection<PlayerDto>> GetProposedPlayersAsync();
    }
}
