using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Models
{
    public class Challenge
    {
        private ChallengeDto _dto;

        public ulong Id => _dto.Id;

        public byte PointsRate => _dto.PointsRate;

        public bool? IsAccepted => _dto.IsAccepted.HasValue
            ? _dto.IsAccepted > 0
            : default(bool?);

        public DateTime? ChallengeDate => _dto.ChallengeDate;

        public int HostPointsDelta { get; }

        public string OpponentLogin { get; }

        public bool Initiated { get; }

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
                return hostLead.Points;
            if (hostLead == null && guestLead != null)
                return -guestLead.Points;
            else if (hostLead?.Points != guestLead?.Points)
            {
                var rate = dto.PointsRate / (decimal)100;
                if (hostLead.Points > guestLead.Points)
                    return (int)Math.Round(hostLead.Points * rate);
                else
                    return (int)Math.Round(-guestLead.Points * rate);
            }
            else
                return 0;
        }
    }
}
