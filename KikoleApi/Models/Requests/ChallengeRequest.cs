using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models.Requests
{
    public class ChallengeRequest
    {
        public ulong GuestUserId { get; set; }

        public byte PointsRate { get; set; }

        internal string IsValid()
        {
            if (GuestUserId == 0)
                return SPA.TextResources.InvalidGuestUserId;

            if (PointsRate == 0 || PointsRate > 100)
                return SPA.TextResources.InvalidPointsRate;

            return null;
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
