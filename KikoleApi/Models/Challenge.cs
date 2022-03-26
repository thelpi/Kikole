using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Challenge
    {
        public ulong Id { get; }

        public string OpponentLogin { get; }

        public byte PointsRate { get; }

        public bool? IsAccepted { get; }

        public bool? IsSuccess { get; }

        public DateTime ChallengeDate { get; }

        public bool Initiated { get; }

        internal Challenge(ChallengeDto dto, string login, ulong userId)
        {
            Id = dto.Id;
            OpponentLogin = login;
            PointsRate = dto.PointsRate;
            IsAccepted = dto.IsAccepted.HasValue
                ? dto.IsAccepted > 0
                : default(bool?);
            IsSuccess = dto.IsSuccess.HasValue
                ? dto.IsSuccess > 0
                : default(bool?);
            ChallengeDate = dto.ChallengeDate;
            Initiated = dto.HostUserId == userId;
        }
    }
}
