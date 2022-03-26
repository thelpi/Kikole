using System;

namespace KikoleSite.Api
{
    public class Challenge
    {
        public ulong Id { get; set; }

        public string OpponentLogin { get; set; }

        public byte PointsRate { get; set; }

        public bool? IsAccepted { get; set; }

        public bool? IsSuccess { get; set; }

        public DateTime ChallengeDate { get; set; }

        public bool Initiated { get; set; }
    }
}
