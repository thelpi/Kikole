using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleApi.Models
{
    public class PlayerRequest
    {
        public string Name { get; set; }

        public DateTime DateOfBirth { get; set; }

        public Country Country { get; set; }

        public Country? SecondCountry { get; set; }

        public DateTime? ProposalDate { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }

        public IReadOnlyList<PlayerClubRequest> Clubs { get; set; }

        internal string IsValid()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "Invalid name";

            if (!Enum.IsDefined(typeof(Country), Country))
                return "Invalid country";

            if (SecondCountry.HasValue)
            {
                if (!Enum.IsDefined(typeof(Country), SecondCountry.Value))
                    return "Invalid second country";

                if (Country == SecondCountry.Value)
                    return "Countries should be different";
            }

            if (!AllowedNames.IsValid())
                return "Invalid allowed names";

            if (Clubs == null || Clubs.Count == 0)
                return "Empty clubs list";

            if (Clubs.Any(c => c?.IsValid() != true))
                return "At least one invalid club";

            var iRange = Enumerable.Range(1, Clubs.Count);

            if (!iRange.All(i => Clubs.Any(c => c.HistoryPosition == i)))
                return "Invalid history positions sequence.";

            if (!iRange.All(i => Clubs.Any(c => c.ImportancePosition == i)))
                return "Invalid importance positions sequence.";

            return null;
        }
    }
}
