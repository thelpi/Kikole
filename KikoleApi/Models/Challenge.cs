using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Challenge
    {
        public ulong Id => _dto.Id;

        public string OpponentLogin { get; }

        public byte PointsRate => _dto.PointsRate;

        public bool? IsAccepted => _dto.IsAccepted.HasValue
            ? _dto.IsAccepted > 0
            : default(bool?);

        public int HostPointsDelta { get; }

        public DateTime ChallengeDate => _dto.ChallengeDate;

        public bool Initiated { get; }

        private ChallengeDto _dto;

        internal Challenge(ChallengeDto dto, string login, ulong userId)
        {
            _dto = dto;
            OpponentLogin = login;
            Initiated = dto.HostUserId == userId;
        }

        internal Challenge(ChallengeDto dto, string login, ulong userId,
            IEnumerable<LeaderDto> leaders)
            : this(dto, login, userId)
        {
            HostPointsDelta = ComputeHostPoints(_dto,
                leaders.SingleOrDefault(l => l.UserId == dto.HostUserId),
                leaders.SingleOrDefault(l => l.UserId == dto.GuestUserId));
        }

        internal static int ComputeHostPoints(ChallengeDto dto,
            LeaderDto hostLead, LeaderDto guestLead)
        {
            if (hostLead != null && guestLead == null)
                return ProposalChart.Default.ChallengeWithdrawalPoints;
            if (hostLead == null && guestLead != null)
                return -ProposalChart.Default.ChallengeWithdrawalPoints;
            else if (hostLead?.Points != guestLead?.Points)
            {
                var rate = dto.PointsRate / (decimal)100;
                if (hostLead.Points > guestLead.Points)
                    return (int)Math.Round(guestLead.Points * rate);
                else
                    return (int)Math.Round(hostLead.Points * rate * -1);
            }
            else
                return 0;
        }
    }
}
