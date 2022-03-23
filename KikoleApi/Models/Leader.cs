using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Leader
    {
        public ulong Id { get; }

        public string Login { get; }

        public uint TotalPoints { get; private set; }

        public TimeSpan BestTime { get; }

        public int SuccessCount { get; }

        public int Position { get; internal set; }

        internal Leader(IGrouping<ulong, LeaderDto> userDtos,
            IReadOnlyCollection<UserDto> users)
        {
            var user = users.Single(p => p.Id == userDtos.Key);
            Login = user.Login;
            SuccessCount = userDtos.Count();
            TotalPoints = (uint)userDtos.Sum(dto => dto.Points);
            BestTime = new TimeSpan(0, userDtos.Min(dto => dto.Time), 0);
            Id = user.Id;
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
                    + (uint)Math.Max(leadersCosting, 0);
            }

            return this;
        }
    }
}
