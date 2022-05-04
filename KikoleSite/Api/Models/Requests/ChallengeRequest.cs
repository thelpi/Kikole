using KikoleSite.Api.Models.Dtos;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Api.Models.Requests
{
    public class ChallengeRequest
    {
        public ulong GuestUserId { get; set; }

        public byte PointsRate { get; set; }

        internal string IsValid(IStringLocalizer resources)
        {
            if (GuestUserId == 0)
                return resources["InvalidGuestUserId"];

            if (PointsRate == 0 || PointsRate > 100)
                return resources["InvalidPointsRate"];

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
