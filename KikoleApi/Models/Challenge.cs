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

        public bool? IsHostSuccess { get; }

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
            var hostLead = leaders.SingleOrDefault(l => l.UserId == _dto.HostUserId);
            var guestLead = leaders.SingleOrDefault(l => l.UserId == _dto.GuestUserId);

            if (hostLead != null && guestLead == null)
                IsHostSuccess = true;
            if (hostLead == null && guestLead != null)
                IsHostSuccess = false;
            else if (hostLead != null && guestLead != null
                && hostLead.Points != guestLead.Points)
            {
                IsHostSuccess = hostLead.Points > guestLead.Points;
            }
        }
    }
}
