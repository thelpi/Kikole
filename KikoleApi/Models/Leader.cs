using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models
{
    public class Leader
    {
        public ulong UserId { get; }

        public string Login { get; }

        public TimeSpan BestTime { get; }

        public int SuccessCount { get; }

        public int TotalPoints { get; private set; }

        public int Position { get; private set; }

        internal Leader(IEnumerable<Leader> group)
        {
            Login = group.First().Login;
            SuccessCount = 0;
            TotalPoints = group.Sum(g => g.TotalPoints);
            BestTime = new TimeSpan(23, 59, 59);
            UserId = group.First().UserId;
        }

        internal Leader(ulong userId, int points,
            IReadOnlyCollection<UserDto> users)
        {
            var user = users.Single(p => p.Id == userId);
            Login = user.Login;
            SuccessCount = 0;
            TotalPoints = points;
            BestTime = new TimeSpan(23, 59, 59);
            UserId = user.Id;
        }

        internal Leader(LeaderDto leader,
            IReadOnlyCollection<UserDto> users)
        {
            var user = users.Single(p => p.Id == leader.UserId);
            Login = user.Login;
            SuccessCount = 1;
            TotalPoints = leader.Points;
            BestTime = new TimeSpan(0, leader.Time, 0);
            UserId = user.Id;
        }

        internal Leader(IGrouping<ulong, LeaderDto> userDtos,
            IReadOnlyCollection<UserDto> users)
        {
            var user = users.Single(p => p.Id == userDtos.Key);
            Login = user.Login;
            SuccessCount = userDtos.Count();
            TotalPoints = userDtos.Sum(dto => dto.Points);
            BestTime = new TimeSpan(0, userDtos.Min(dto => dto.Time), 0);
            UserId = user.Id;
        }

        internal Leader WithPointsFromSubmittedPlayers(
            IEnumerable<DateTime> dates,
            IEnumerable<LeaderDto> datesLeaders)
        {
            foreach (var date in dates)
            {
                var leadersCosting = ProposalChart.Default.SubmissionBonusPoints - datesLeaders
                    .Where(d => d.ProposalDate.Date == date && d.Points >= ProposalChart.Default.SubmissionThresholdlosePoints)
                    .Sum(d => ProposalChart.Default.SubmissionLosePointsByLeader);

                TotalPoints += ProposalChart.Default.SubmissionBasePoints
                    + Math.Max(leadersCosting, 0);
            }

            return this;
        }

        internal Leader WithPointsFromChallenge(int challengePoints, bool hostWin)
        {
            TotalPoints += challengePoints * (hostWin ? 1 : -1);
            return this;
        }

        internal static IReadOnlyCollection<Leader> DoubleSortWithPosition(
            IEnumerable<Leader> leaders, LeaderSorts sort)
        {
            return sort == LeaderSorts.TotalPoints
                ? leaders.SetPositions(l => l.TotalPoints, l => l.BestTime.TotalMinutes, true, false, (l, i) => l.Position = i)
                : leaders.SetPositions(l => l.BestTime.TotalMinutes, l => l.TotalPoints, false, true, (l, i) => l.Position = i);
        }

        internal static IReadOnlyCollection<Leader> SortWithPosition(
            IEnumerable<Leader> leaders, LeaderSorts sort)
        {
            switch (sort)
            {
                case LeaderSorts.SuccessCount:
                    leaders = leaders.SetPositions(l => l.SuccessCount, true, (l, i) => l.Position = i);
                    break;
                case LeaderSorts.BestTime:
                    leaders = leaders.SetPositions(l => l.BestTime, false, (l, i) => l.Position = i);
                    break;
                case LeaderSorts.TotalPoints:
                    leaders = leaders.SetPositions(l => l.TotalPoints, true, (l, i) => l.Position = i);
                    break;
            }

            return leaders.ToList();
        }
    }
}
