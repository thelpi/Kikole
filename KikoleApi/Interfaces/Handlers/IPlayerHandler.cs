using System;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces.Handlers
{
    /// <summary>
    /// Player handler interface.
    /// </summary>
    public interface IPlayerHandler
    {
        /// <summary>
        /// Gets the player proposed at a specified date, with full info.
        /// </summary>
        /// <param name="date">Proposed date.</param>
        /// <returns>Player with full info.</returns>
        Task<PlayerFullDto> GetPlayerOfTheDayFullInfoAsync(DateTime date);

        /// <summary>
        /// Gets the player with full info by its root data.
        /// </summary>
        /// <param name="player">Player base data.</param>
        /// <returns>Player with full info.</returns>
        Task<PlayerFullDto> GetPlayerFullInfoAsync(PlayerDto player);
    }
}
