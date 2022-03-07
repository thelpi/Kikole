using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class PlayerRequest
    {
        public string Name { get; set; }

        public ushort YearOfBirth { get; set; }
        
        public Country Country { get; set; }

        public DateTime? ProposalDate { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }

        public IReadOnlyList<ulong> Clubs { get; set; }

        public string Clue { get; set; }

        internal string IsValid()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "Invalid name";

            if (!Enum.IsDefined(typeof(Country), Country))
                return "Invalid country";

            if (YearOfBirth < 1900 || YearOfBirth > 2100)
                return "Invalid year of birth";

            if (!AllowedNames.IsValid())
                return "Invalid allowed names";

            if (Clubs == null || Clubs.Count == 0)
                return "Empty clubs list";

            if (Clubs.Any(c => c <= 0))
                return "At least one invalid club";

            if (string.IsNullOrWhiteSpace(Clue))
                return "Invalid clue";

            return null;
        }

        internal PlayerDto ToDto()
        {
            return new PlayerDto
            {
                CountryId = (ulong)Country,
                Name = Name,
                ProposalDate = ProposalDate,
                YearOfBirth = YearOfBirth,
                AllowedNames = AllowedNames.SanitizeJoin(Name),
                Clue = Clue
            };
        }

        internal IReadOnlyList<PlayerClubDto> ToPlayerClubDtos(ulong playerId)
        {
            return Clubs
                .Select((c, i) => new PlayerClubDto
                {
                    HistoryPosition = (byte)(i + 1),
                    ClubId = c,
                    PlayerId = playerId
                })
                .ToList();
        }
    }
}
