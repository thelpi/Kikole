using System;
using System.Threading.Tasks;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Interfaces.Services
{
    /// <summary>
    /// Leader service interface.
    /// </summary>
    public interface ILeaderService
    {
        /// <summary>
        /// Gets leaderboard for a given period.
        /// </summary>
        /// <param name="startDate">Starting date.</param>
        /// <param name="endDate">Ending date.</param>
        /// <param name="leaderSort">Sort type.</param>
        /// <returns>Leaderboard.</returns>
        Task<Leaderboard> GetLeaderboardAsync(DateTime startDate, DateTime endDate, LeaderSorts leaderSort);

        /// <summary>
        /// Gets the board for a single day.
        /// </summary>
        /// <param name="date">The day.</param>
        /// <param name="sort">Sort for leaders.</param>
        /// <returns>Day board.</returns>
        Task<Dayboard> GetDayboardAsync(DateTime day, DayLeaderSorts sort);

        /// <summary>
        /// Get user statistics.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>User statistics; <c>Null</c> if user doesn't exist.</returns>
        Task<UserStat> GetUserStatisticsAsync(ulong userId);

        /// <summary>
        /// Computes missing leaders (administration tool).
        /// </summary>
        /// <returns>Nothing.</returns>
        Task ComputeMissingLeadersAsync();
    }
}
