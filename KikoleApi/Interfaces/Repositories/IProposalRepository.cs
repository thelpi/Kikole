using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces.Repositories
{
    /// <summary>
    /// Proposal repository interface.
    /// </summary>
    public interface IProposalRepository
    {
        /// <summary>
        /// Creates a proposal.
        /// </summary>
        /// <param name="proposal">Proposal to create.</param>
        /// <returns>Proposal identifier.</returns>
        Task<ulong> CreateProposalAsync(ProposalDto proposal);

        /// <summary>
        /// Gets proposals of a single user for a player.
        /// </summary>
        /// <param name="playerProposalDate">Player's proposal date.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of proposals.</returns>
        Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime playerProposalDate, ulong userId);

        /// <summary>
        /// Gets proposals of a single user for a player, only the day the player has been proposed.
        /// </summary>
        /// <param name="playerProposalDate">Player's proposal date.</param>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of proposals.</returns>
        Task<IReadOnlyCollection<ProposalDto>> GetProposalsDateExactAsync(DateTime playerProposalDate, ulong userId);

        /// <summary>
        /// Gets proposals of a single user, for any player, but only on dates that match the player's proposal date.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>Collection of proposals.</returns>
        Task<IReadOnlyCollection<ProposalDto>> GetAllProposalsDateExactAsync(ulong userId);

        /// <summary>
        /// Gets successful name' proposals, for every user, only the day the player has been proposed.
        /// </summary>
        /// <param name="playerProposalDate">Player's proposal date.</param>
        /// <returns>Collection of proposals.</returns>
        Task<IReadOnlyCollection<ProposalDto>> GetWiningProposalsAsync(DateTime playerProposalDate);

        /// <summary>
        /// Gets every proposal made by every user, only the day the player has been proposed.
        /// </summary>
        /// <param name="playerProposalDate">Player's proposal date.</param>
        /// <returns>Collection of proposals.</returns>
        Task<IReadOnlyCollection<ProposalDto>> GetProposalsAsync(DateTime playerProposalDate);
    }
}
