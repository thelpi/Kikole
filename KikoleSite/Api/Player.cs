using System;
using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class Player : PlayerCreator
    {
        public ulong Id { get; set; }

        public IReadOnlyCollection<PlayerClub> Clubs { get; set; }

        public IReadOnlyCollection<string> AllowedNames { get; set; }

        public ushort YearOfBirth { get; set; }

        public ulong Country { get; set; }

        public DateTime? ProposalDate { get; set; }

        public string Clue { get; set; }

        public ulong? Badge { get; set; }

        public ulong Position { get; set; }

        public DateTime? RejectDate { get; set; }
    }
}
