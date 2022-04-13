using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;

namespace KikoleApi.Interfaces.Services
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
        /// <param name="withFirst">Include the first special date y/n.</param>
        /// <returns>Date of first player submitted.</returns>
        Task<DateTime> GetFirstSubmittedPlayerDateAsync(bool withFirst);

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
        /// <param name="isEasy"><c>True</c> to get easy clue; otherwise normal clue.</param>
        /// <returns>Clue.</returns>
        Task<string> GetPlayerClueAsync(DateTime proposalDate, bool isEasy);

        /// <summary>
        /// Accepts a player submission.
        /// </summary>
        /// <param name="request">The submittion request.</param>
        /// <param name="currentClue">The current clue on the player (en).</param>
        /// <param name="currentEasyClue">The current easy clue on the player (en).</param>
        /// <returns>Nothing.</returns>
        Task AcceptSubmittedPlayerAsync(PlayerSubmissionValidationRequest request, string currentClue, string currentEasyClue);

        /// <summary>
        /// Computes a challenge date by checking player submissions by host and guest.
        /// </summary>
        /// <param name="challenge">Challenge info</param>
        /// <param name="hostDates">Host non available dates.</param>
        /// <param name="guestDates">Guest non available dates.</param>
        /// <returns>First date when the challenge is possible.</returns>
        Task<DateTime> ComputeAvailableChallengeDateAsync(
            ChallengeDto challenge,
            IReadOnlyCollection<DateTime> hostDates,
            IReadOnlyCollection<DateTime> guestDates);

        /// <summary>
        /// Gets <see cref="PlayerCreator"/> at a specified date from the point of view of a specified user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="proposalDate">Date of the player.</param>
        /// <returns>Instance of <see cref="PlayerCreator"/>.</returns>
        Task<PlayerCreator> GetPlayerOfTheDayFromUserPovAsync(ulong userId, DateTime proposalDate);

        /// <summary>
        /// Gets pending player submissions.
        /// </summary>
        /// <returns>Collection of <see cref="Player"/>.</returns>
        Task<IReadOnlyCollection<Player>> GetPlayerSubmissionsAsync();

        /// <summary>
        /// Gets known player names for a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of player' names.</returns>
        Task<IReadOnlyCollection<string>> GetKnownPlayerNamesAsync(ulong userId);

        /// <summary>
        /// Valides a player submission.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <returns>Error value.</returns>
        Task<PlayerSubmissionErrors> ValidatePlayerSubmissionAsync(PlayerSubmissionValidationRequest request);
    }
}
