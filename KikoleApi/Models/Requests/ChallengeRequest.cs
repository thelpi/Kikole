using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models.Requests
{
    public class ChallengeRequest
    {
        public ulong GuestUserId { get; set; }

        public byte PointsRate { get; set; }

        internal bool IsValid()
        {
            return GuestUserId > 0
                && PointsRate > 0
                && PointsRate <= 100;
        }

        internal ChallengeDto ToDto(ulong hostUserId)
        {
            return new ChallengeDto
            {
                GuestUserId = GuestUserId,
                HostUserId = hostUserId,
                PointsRate = PointsRate
            };
        }
    }
}
