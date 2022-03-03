using System;

namespace KikoleApi.Domain.Models
{
    public class PlayerRequest
    {
        public string Name { get; set; }

        public DateTime DateOfBirth { get; set; }

        public Country Country { get; set; }

        public Country? SecondCountry { get; set; }

        public DateTime? ProposalDate { get; set; }

        public string[] AllowedNames { get; set; }

        public ClubRequest[] Clubs { get; set; }

        public class ClubRequest
        {
            public string Name { get; set; }

            public byte HistoryPosition { get; set; }

            public byte ImportancePosition { get; set; }

            public string[] AllowedNames { get; set; }
        }
    }
}
