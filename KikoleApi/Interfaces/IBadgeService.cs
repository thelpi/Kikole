using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;

namespace KikoleApi.Interfaces
{
    public interface IBadgeService
    {
        /// <summary>
        /// Prepares badges triggered by a user finding a player.
        /// </summary>
        /// <param name="leader">The leader instance related to the user and player (fake it if the player found is not from today).</param>
        /// <param name="playerOfTheDay">The player found.</param>
        /// <param name="proposalsBeforeWin">Proposals made by the user BEFORE finding the player.</param>
        /// <param name="isActualTodayleader"><c>True</c> if the player found is from today.</param>
        /// <returns>Collection of <see cref="UserBadge"/>.</returns>
        Task<IReadOnlyCollection<UserBadge>> PrepareNewLeaderBadgesAsync(
            LeaderDto leader,
            PlayerDto playerOfTheDay,
            IReadOnlyCollection<ProposalDto> proposalsBeforeWin,
            bool isActualTodayleader);

        /// <summary>
        /// Prepares badges triggered by a user making a proposal.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="request">Proposal request.</param>
        /// <returns>Collection of <see cref="UserBadge"/>.</returns>
        Task<IReadOnlyCollection<UserBadge>> PrepareNonLeaderBadgesAsync(
            ulong userId,
            BaseProposalRequest request);

        /// <summary>
        /// Add a badge to a user, if the user does not have the badge already and the badge is not unique.
        /// </summary>
        /// <param name="badge">Badge to add.</param>
        /// <param name="userId">User who get the badge.</param>
        /// <returns><c>True</c> if badge added.</returns>
        Task<bool> AddBadgeToUserAsync(
            Badges badge,
            ulong userId);

        /// <summary>
        /// Gets all badges.
        /// </summary>
        /// <returns>Collection of <see cref="Badge"/> sorted by <see cref="Badge.Users"/> descending.</returns>
        Task<IReadOnlyCollection<Badge>> GetAllBadgesAsync();

        /// <summary>
        /// Gets every badge of a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="isAllowedToSeeHiddenBadge"><c>True</c> if the user can see hidden badges.</param>
        /// <returns>Collection of <see cref="UserBadge"/> sorted by rareness.</returns>
        Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(
            ulong userId,
            bool isAllowedToSeeHiddenBadge);

        /// <summary>
        /// Resets and recomputes datas on every badge.
        /// </summary>
        /// <returns>Nothing.</returns>
        Task ResetBadgesAsync();
    }
}
