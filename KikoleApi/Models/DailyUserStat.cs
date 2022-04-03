using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class DailyUserStat
    {
        public DateTime Date { get; }

        public string Answer { get; }

        public bool Attempt { get; }

        public TimeSpan? Time { get; }

        public int? Points { get; }

        public int? TimePosition { get; }

        public int? PointsPosition { get; }

        internal DailyUserStat(DateTime currentDate,
            string playerName,
            IReadOnlyCollection<LeaderDto> leaders)
        {
            Date = currentDate;
            Answer = playerName;
            Points = Leader.GetSubmittedPlayerPoints(leaders, currentDate);
        }

        internal DailyUserStat(ulong userId,
            DateTime currentDate,
            string playerName,
            bool attempt,
            IReadOnlyCollection<LeaderDto> leaders,
            LeaderDto meLeader)
        {
            Date = currentDate;
            Answer = playerName;
            Attempt = attempt;
            Points = meLeader != null
                ? meLeader.Points
                : default(int?);
            Time = meLeader != null
                ? new TimeSpan(0, meLeader.Time, 0)
                : default(TimeSpan?);
            PointsPosition = GetUserPositionInLeaders(
                userId, leaders.OrderByDescending(t => t.Points));
            TimePosition = GetUserPositionInLeaders(
                userId, leaders.OrderBy(t => t.Time));
        }

        private static int? GetUserPositionInLeaders(
            ulong userId, IEnumerable<LeaderDto> orderedLeaders)
        {
            var tIndex = -1;
            var i = 0;
            foreach (var orderedLeader in orderedLeaders)
            {
                if (orderedLeader.UserId == userId)
                {
                    tIndex = i + 1;
                    break;
                }
                i++;
            }

            return tIndex == -1
                ? default(int?)
                : tIndex;
        }
    }
}
