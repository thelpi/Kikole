using System;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Requests;

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
        /// Gets every information about a player.
        /// </summary>
        /// <param name="p">Player base information.</param>
        /// <returns>Information about the player.</returns>
        Task<PlayerFullDto> GetPlayerInfoAsync(PlayerDto p);

        /// <summary>
        /// Gets the date of the first player submitted.
        /// </summary>
        /// <returns>Date of first player submitted.</returns>
        Task<DateTime> GetFirstSubmittedPlayerDateAsync();

        /// <summary>
        /// Creates a full player.
        /// </summary>
        /// <param name="request">Player request.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Player identifier.</returns>
        Task<ulong> CreatePlayerAsync(PlayerRequest request, ulong userId);

        /// <summary>
        /// Gets the club, in the proper language related to the player at the specified date.
        /// </summary>
        /// <param name="proposalDate">Date of player proposal.</param>
        /// <returns>Clue.</returns>
        Task<string> GetPlayerClueAsync(DateTime proposalDate);

        /// <summary>
        /// Accepts a player submission.
        /// </summary>
        /// <param name="request">The submittion request.</param>
        /// <param name="currentClue">The current clue on the player (en).</param>
        /// <returns>Nothing.</returns>
        Task AcceptSubmittedPlayerAsync(PlayerSubmissionValidationRequest request, string currentClue);
    }
}
