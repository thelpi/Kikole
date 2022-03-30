using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class PlayerRequest
    {
        public string Name { get; set; }

        public ushort YearOfBirth { get; set; }
        
        public Countries Country { get; set; }

        public DateTime? ProposalDate { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }

        public IReadOnlyList<ulong> Clubs { get; set; }

        public string Clue { get; set; }

        public Positions Position { get; set; }

        public bool SetLatestProposalDate { get; set; }

        public bool HideCreator { get; set; }

        internal string IsValid(DateTime now)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "Invalid name";

            if (!Enum.IsDefined(typeof(Countries), Country))
                return "Invalid country";

            if (!Enum.IsDefined(typeof(Positions), Position))
                return "Invalid position";

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

            if (ProposalDate.HasValue && ProposalDate.Value.Date < now.Date)
                return "Invalid proposal date";

            return null;
        }

        internal PlayerDto ToDto(ulong userId)
        {
            return new PlayerDto
            {
                CountryId = (ulong)Country,
                Name = Name,
                ProposalDate = ProposalDate,
                YearOfBirth = YearOfBirth,
                AllowedNames = AllowedNames.SanitizeJoin(Name),
                Clue = Clue,
                PositionId = (ulong)Position,
                CreationUserId = userId,
                HideCreator = (byte)(HideCreator ? 1 : 0)
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
