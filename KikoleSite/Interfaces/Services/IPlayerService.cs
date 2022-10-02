using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;

namespace KikoleSite.Interfaces.Services
{
    /// <summary>
    /// Player service interface.
    /// </summary>
    public interface IPlayerService
    {
        /// <summary>
        /// Gets the player proposed at a specified date, with full info.
        /// </summary>
        /// <param name="date">Proposed date.</param>
        /// <returns>Player with full info.</returns>
        Task<PlayerFullDto> GetPlayerOfTheDayFullInfoAsync(DateTime date);

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
        /// <param name="language">The language.</param>
        /// <returns>Clue.</returns>
        Task<string> GetPlayerClueAsync(DateTime proposalDate, bool isEasy, Languages language);

        /// <summary>
        /// Accepts a player submission.
        /// </summary>
        /// <param name="request">The submittion request.</param>
        /// <param name="currentClue">The current clue on the player (en).</param>
        /// <param name="currentEasyClue">The current easy clue on the player (en).</param>
        /// <returns>Nothing.</returns>
        Task AcceptSubmittedPlayerAsync(PlayerSubmissionValidationRequest request, string currentClue, string currentEasyClue);

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
        /// <returns>Error value, user involved and collection of badges to manage (if no error).</returns>
        Task<(PlayerSubmissionErrors, ulong, IReadOnlyCollection<Badges>)> ValidatePlayerSubmissionAsync(PlayerSubmissionValidationRequest request);

        /// <summary>
        /// Randomize the assignement of every player's proposal date, starting tomorrow.
        /// </summary>
        /// <remarks>Nothing happens if tomorrow is in less than 30 minutes.</remarks>
        /// <returns>Asynchronous.</returns>
        Task ReassignPlayersOfTheDayAsync();

        /// <summary>
        /// Updates all clues in every langugage for a player.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="clue">Standard clue, in english.</param>
        /// <param name="easyClue">Easy clue, in english.</param>
        /// <param name="clueLanguages">Standard clue, in another languages.</param>
        /// <param name="easyClueLanguages">Easy clue, in another languages.</param>
        /// <returns>Nothing.</returns>
        Task UpdatePlayerCluesAsync(ulong playerId, string clue, string easyClue,
            IReadOnlyDictionary<Languages, string> clueLanguages,
            IReadOnlyDictionary<Languages, string> easyClueLanguages);

        /// <summary>
        /// Ges both clues in every language specified for of a player.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="languages">Languages to collect.</param>
        /// <returns>Both clues in every language.</returns>
        Task<IReadOnlyDictionary<Languages, (string clue, string easyclue)>> GetPlayerCluesAsync(ulong playerId, IReadOnlyCollection<Languages> languages);
    }
}
