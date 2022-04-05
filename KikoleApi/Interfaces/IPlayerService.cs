using System;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IPlayerService
    {
        /// <summary>
        /// Gets every information about a player.
        /// </summary>
        /// <param name="date">Date of player proposal.</param>
        /// <returns>Information about the player.</returns>
        Task<PlayerFullDto> GetPlayerInfoAsync(DateTime date);

        /// <summary>
        /// Gets the date of the first player submitted.
        /// </summary>
        /// <returns>Date of first player submitted.</returns>
        Task<DateTime> GetFirstSubmittedPlayerDateAsync();
    }
}
