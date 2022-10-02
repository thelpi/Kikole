using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;

namespace KikoleSite.Interfaces.Services
{
    /// <summary>
    /// Badge service interface.
    /// </summary>
    public interface IBadgeService
    {
        /// <summary>
        /// Prepares badges triggered by a user finding a player.
        /// </summary>
        /// <param name="leader">The leader instance related to the user and player (fake it if the player found is not from today).</param>
        /// <param name="playerOfTheDay">The player found.</param>
        /// <param name="proposalsBeforeWin">Proposals made by the user BEFORE finding the player.</param>
        /// <param name="language">User language.</param>
        /// <returns>Collection of <see cref="UserBadge"/>.</returns>
        Task<IReadOnlyCollection<UserBadge>> PrepareNewLeaderBadgesAsync(
            LeaderDto leader,
            PlayerDto playerOfTheDay,
            IReadOnlyCollection<ProposalDto> proposalsBeforeWin,
            Languages language);

        /// <summary>
        /// Prepares badges triggered by a user making a proposal.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="request">Proposal request.</param>
        /// <param name="language">User language.</param>
        /// <returns>Collection of <see cref="UserBadge"/>.</returns>
        Task<IReadOnlyCollection<UserBadge>> PrepareNonLeaderBadgesAsync(
            ulong userId,
            BaseProposalRequest request,
            Languages language);

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
        /// <param name="language">User language.</param>
        /// <returns>Collection of <see cref="Badge"/> sorted by <see cref="Badge.Users"/> descending.</returns>
        Task<IReadOnlyCollection<Badge>> GetAllBadgesAsync(Languages language);

        /// <summary>
        /// Gets every badge of a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="connectedUserId">User identifier who request information.</param>
        /// <param name="language">User language.</param>
        /// <returns>Collection of <see cref="UserBadge"/> sorted by rareness.</returns>
        Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(ulong userId, ulong connectedUserId, Languages language);

        /// <summary>
        /// Resets and recomputes datas on every badge.
        /// </summary>
        /// <returns>Nothing.</returns>
        Task ResetBadgesAsync(Languages language);
    }
}
