using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models.Requests
{
    public class ChallengeRequest
    {
        public ulong GuestUserId { get; set; }

        public byte PointsRate { get; set; }

        internal string IsValid(TextResources resources)
        {
            if (GuestUserId == 0)
                return resources.InvalidGuestUserId;

            if (PointsRate == 0 || PointsRate > 100)
                return resources.InvalidPointsRate;

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
