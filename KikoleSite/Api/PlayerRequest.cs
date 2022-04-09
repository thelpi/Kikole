using System;
using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class PlayerRequest
    {
        public string Name { get; set; }

        public ushort YearOfBirth { get; set; }
        
        public string Country { get; set; }

        public DateTime? ProposalDate { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }

        public IReadOnlyList<ulong> Clubs { get; set; }

        public string Position { get; set; }

        public bool SetLatestProposalDate { get; set; }

        public string ClueEn { get; set; }

        public string EasyClueEn { get; set; }

        public IReadOnlyDictionary<Languages, string> ClueLanguages { get; set; }

        public IReadOnlyDictionary<Languages, string> EasyClueLanguages { get; set; }

        public bool HideCreator { get; set; }
    }
}
