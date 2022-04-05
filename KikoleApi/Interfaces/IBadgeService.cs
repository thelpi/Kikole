using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
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
            LeaderDto leader, PlayerDto playerOfTheDay,
            IReadOnlyCollection<ProposalDto> proposalsBeforeWin,
            bool isActualTodayleader);

        /// <summary>
        /// Prepares badges triggered by a user making a proposal.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="request">Proposal request.</param>
        /// <returns>Collection of <see cref="UserBadge"/>.</returns>
        Task<IReadOnlyCollection<UserBadge>> PrepareNonLeaderBadgesAsync(
            ulong userId, BaseProposalRequest request);
    }
}
