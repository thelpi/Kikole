using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models;
using KikoleApi.Models.Enums;

namespace KikoleApi.Interfaces.Services
{
    public interface ILeaderService
    {
        /// <summary>
        /// Gets leaders for a specified day with a particular sort.
        /// </summary>
        /// <param name="day">The requested day.</param>
        /// <param name="sort">Th expected sort.</param>
        /// <returns>Collection of sorted leaders for the day.</returns>
        Task<IReadOnlyCollection<Leader>> GetLeadersOfTheDayAsync(DateTime day, LeaderSorts sort);

        /// <summary>
        /// Gets challenge leaders for a given period.
        /// </summary>
        /// <param name="minimalDate">Starting date.</param>
        /// <param name="maximalDate">Ending date.</param>
        /// <returns>Leaders sorted by challenge points.</returns>
        Task<IReadOnlyCollection<Leader>> GetPvpLeadersAsync(DateTime? minimalDate, DateTime? maximalDate);

        /// <summary>
        /// Gets leaders for a given period; challenge points excluded.
        /// </summary>
        /// <param name="minimalDate">Starting date.</param>
        /// <param name="maximalDate">Ending date.</param>
        /// <param name="leaderSort">Sort type.</param>
        /// <returns>Sorted leaders.</returns>
        Task<IReadOnlyCollection<Leader>> GetPveLeadersAsync(DateTime? minimalDate, DateTime? maximalDate, LeaderSorts leaderSort);

        /// <summary>
        /// Gets monthly awards
        /// </summary>
        /// <param name="year">Year.</param>
        /// <param name="month">Month.</param>
        /// <returns>Instance of <see cref="Awards"/>.</returns>
        Task<Awards> GetAwardsAsync(int year, int month);

        /// <summary>
        /// Get user statistics.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>User statistics; <c>Null</c> if user doesn't exist.</returns>
        Task<UserStat> GetUserStatisticsAsync(ulong userId);
    }
}
