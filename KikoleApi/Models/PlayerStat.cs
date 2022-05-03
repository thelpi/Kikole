using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    /// <summary>
    /// Statistics about a player.
    /// </summary>
    public class PlayerStat
    {
        /// <summary>
        /// Player name; <c>null</c> if anonymised.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creator login; <c>null</c> if anonymised.
        /// </summary>
        public string CreatorLogin { get; }

        /// <summary>
        /// Proposal date.
        /// </summary>
        public DateTime ProposalDate { get; }

        /// <summary>
        /// Count of users who made an attempt.
        /// </summary>
        public int AttemptCount { get; }

        /// <summary>
        /// Count of users who found the player.
        /// </summary>
        public int SuccessCount { get; }

        /// <summary>
        /// Success rate (0 to 1).
        /// </summary>
        public decimal SuccessRate => AttemptCount == 0 ? 0 : SuccessCount / (decimal)AttemptCount;

        /// <summary>
        /// Average points when found.
        /// </summary>
        public int? AveragePoints { get; }

        /// <summary>
        /// Minimal points when found.
        /// </summary>
        public int? MinPoints { get; }

        /// <summary>
        /// Maximal points when found.
        /// </summary>
        public int? MaxPoints { get; }

        /// <summary>
        /// Average time to found (<c>null</c> if no one found).
        /// </summary>
        public int? AverageTimeSec => AverageTime.ToSeconds();

        /// <summary>
        /// Minimal time to found (<c>null</c> if no one found).
        /// </summary>
        public int? MinTimeSec => MinTime.ToSeconds();

        /// <summary>
        /// Maximal time to found (<c>null</c> if no one found).
        /// </summary>
        public int? MaxTimeSec => MaxTime.ToSeconds();

        internal TimeSpan? AverageTime { get; }
        internal TimeSpan? MinTime { get; }
        internal TimeSpan? MaxTime { get; }

        // "creatorLogin" should be null if it's today's player and "HideCreator" is true
        internal PlayerStat(PlayerDto player,
            string creatorLogin,
            bool anonymise,
            IReadOnlyCollection<LeaderDto> leaders,
            int attemptCount)
        {
            Name = anonymise ? null : player.Name;
            CreatorLogin = creatorLogin;
            ProposalDate = player.ProposalDate.Value;
            AttemptCount = attemptCount;
            SuccessCount = leaders.Count;
            if (leaders.Count > 0)
            {
                AveragePoints = (int)leaders.Average(_ => _.Points);
                MinPoints = leaders.Min(_ => _.Points);
                MaxPoints = leaders.Max(_ => _.Points);
                AverageTime = new TimeSpan(0, (int)Math.Ceiling(leaders.Average(_ => _.Time)), 0);
                MinTime = new TimeSpan(0, leaders.Min(_ => _.Time), 0);
                MaxTime = new TimeSpan(0, leaders.Max(_ => _.Time), 0);
            }
        }
    }
}
