using System;

namespace KikoleApi.Models.Dtos
{
    public class ChallengeDto : BaseDto
    {
        public ulong HostUserId { get; set; }

        public ulong GuestUserId { get; set; }

        public byte? IsAccepted { get; set; }

        public DateTime ChallengeDate { get; set; }

        public byte PointsRate { get; set; }
    }
}
