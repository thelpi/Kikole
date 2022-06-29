using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Models
{
    public class Leader
    {
        public ulong UserId { get; }

        public string Login { get; }

        public TimeSpan BestTime { get; }

        public int SuccessCount { get; }

        public int CreatedCount { get; }

        public int TotalPoints { get; private set; }

        public int Position { get; internal set; }

        internal DateTime BestTimeDate { get; }

        internal Leader(UserDto user, DateTime date,
            IReadOnlyCollection<LeaderDto> leaders)
        {
            UserId = user.Id;
            Login = user.Login;
            BestTime = new TimeSpan(23, 59, 59);
            TotalPoints = GetSubmittedPlayerPoints(leaders, date);
        }

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
            BestTimeDate = leader.ProposalDate;
        }

        internal Leader(IGrouping<ulong, LeaderDto> userDtos,
            IReadOnlyCollection<UserDto> users,
            int createdCount)
        {
            var user = users.Single(p => p.Id == userDtos.Key);
            Login = user.Login;
            SuccessCount = userDtos.Count();
            CreatedCount = createdCount;
            TotalPoints = userDtos.Sum(dto => dto.Points);
            BestTime = new TimeSpan(0, userDtos.Min(dto => dto.Time), 0);
            UserId = user.Id;
            BestTimeDate = userDtos.First(ud => ud.Time == userDtos.Min(dto => dto.Time)).ProposalDate;
        }

        internal Leader WithPointsFromSubmittedPlayers(
            IEnumerable<DateTime> dates,
            IEnumerable<LeaderDto> datesLeaders)
        {
            foreach (var date in dates)
                TotalPoints += GetSubmittedPlayerPoints(datesLeaders, date);

            return this;
        }

        internal static int GetSubmittedPlayerPoints(IEnumerable<LeaderDto> datesLeaders, DateTime date)
        {
            // ONLY DAY ONE COST POINTS
            var leadersCosting = ProposalChart.Default.SubmissionBonusPoints
                - datesLeaders
                    .Where(d => d.ProposalDate.Date == date
                        && d.Points >= ProposalChart.Default.SubmissionThresholdlosePoints
                        && d.IsCurrentDay)
                    .Sum(d => ProposalChart.Default.SubmissionLosePointsByLeader);

            return ProposalChart.Default.SubmissionBasePoints
                + Math.Max(leadersCosting, 0);
        }

        internal Leader WithPointsFromChallenge(int challengePoints, bool hostWin)
        {
            TotalPoints += challengePoints * (hostWin ? 1 : -1);
            return this;
        }
    }
}
