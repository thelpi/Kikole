using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces.Repositories
{
    /// <summary>
    /// Player repository interface.
    /// </summary>
    public interface IPlayerRepository
    {
        /// <summary>
        /// Creates a player.
        /// </summary>
        /// <param name="player">Player DTO.</param>
        /// <returns>Player identifier generated.</returns>
        Task<ulong> CreatePlayerAsync(PlayerDto player);

        /// <summary>
        /// Creates a link between a player and a club.
        /// </summary>
        /// <param name="playerClub">Player club DTO.</param>
        /// <returns>Asynchronous.</returns>
        Task CreatePlayerClubsAsync(PlayerClubDto playerClub);

        /// <summary>
        /// Gets the player proposed at a specified date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The player.</returns>
        Task<PlayerDto> GetPlayerOfTheDayAsync(DateTime date);

        /// <summary>
        /// Gets players proposed in a range of dates.
        /// </summary>
        /// <param name="minimalDate">Starting date; <c>null</c> to get from the whole past.</param>
        /// <param name="maximalDate">Ending date; <c>null</c> to get from the whole future.</param>
        /// <returns>Collection of players.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetPlayersOfTheDayAsync(DateTime? minimalDate, DateTime? maximalDate);

        /// <summary>
        /// Gets a player by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Player.</returns>
        Task<PlayerDto> GetPlayerByIdAsync(ulong id);

        /// <summary>
        /// Gets club career of a player.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Collection of player clubs.</returns>
        Task<IReadOnlyList<PlayerClubDto>> GetPlayerClubsAsync(ulong playerId);

        /// <summary>
        /// Gets the proposal date of the furthest player from now.
        /// </summary>
        /// <returns>Furthest proposal date.</returns>
        Task<DateTime> GetLatestProposalDateAsync();

        /// <summary>
        /// Gets player' names known by the specified user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of player' names.</returns>
        Task<IReadOnlyCollection<string>> GetKnownPlayerNamesAsync(ulong userId);

        /// <summary>
        /// Validates the submission of a player.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="clueEn">Clue override, in english.</param>
        /// <param name="easyClueEn">Easy clue override, in english.</param>
        /// <param name="date">Date of proposal.</param>
        /// <returns>Asynchronous.</returns>
        Task ValidatePlayerProposalAsync(ulong playerId, string clueEn, string easyClueEn, DateTime date);

        /// <summary>
        /// Changes the proposal date of a player.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="date">New proposal date.</param>
        /// <returns>Asynchronous.</returns>
        Task ChangePlayerProposalDateAsync(ulong playerId, DateTime date);

        /// <summary>
        /// Inserts clues for a player in different languages.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="isEasy"><c>True</c> if easy clue.</param>
        /// <param name="cluesByLanguage">Clues for every language except english.</param>
        /// <returns>Asynchronous.</returns>
        Task InsertPlayerCluesByLanguageAsync(ulong playerId, byte isEasy, IReadOnlyDictionary<ulong, string> cluesByLanguage);

        /// <summary>
        /// Gets submitted players waiting for validation.
        /// </summary>
        /// <returns>Collection of players.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetPendingValidationPlayersAsync();

        /// <summary>
        /// Refuses a player submission.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Asynchronous.</returns>
        Task RefusePlayerProposalAsync(ulong playerId);

        /// <summary>
        /// Gets the clue for a player in a specific langugage.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="isEasy"><c>True</c> for easy clue.</param>
        /// <param name="languageId">Language identifier except english.</param>
        /// <returns>The clue.</returns>
        Task<string> GetClueAsync(ulong playerId, byte isEasy, ulong languageId);

        /// <summary>
        /// Gets every player created by a specified user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="accepted"><c>True</c> to get accepted players; <c>false</c> to get refused players; <c>null</c> to get all.</param>
        /// <returns>Collection of players.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetPlayersByCreatorAsync(ulong userId, bool? accepted);

        /// <summary>
        /// Gets the date of the first player proposed; exclude special players.
        /// </summary>
        /// <returns>Date of the first proposed player.</returns>
        Task<DateTime> GetFirstDateAsync();
    }
}
