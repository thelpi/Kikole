using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models
{
    public class Player : PlayerCreator
    {
        internal const ulong ImportantId = 81;

        public ulong Id { get; }

        public IReadOnlyCollection<PlayerClub> Clubs { get; }

        public IReadOnlyCollection<string> AllowedNames { get; }

        public ushort YearOfBirth { get; }

        public Countries Country { get; }

        public DateTime? ProposalDate { get; }

        public string Clue { get; }

        public Badges? Badge { get; }

        public Positions Position { get; }

        public DateTime? RejectDate { get; }

        internal Player(PlayerDto p,
            IEnumerable<UserDto> users,
            IEnumerable<PlayerClubDto> playerClubs,
            IEnumerable<ClubDto> playerClubsDetails)
            : base(users.Single(u => u.Id == p.CreationUserId), p)
        {
            Id = p.Id;
            ProposalDate = p.ProposalDate;
            RejectDate = p.RejectDate;
            AllowedNames = p.AllowedNames.Disjoin();
            Badge = (Badges?)p.BadgeId;
            Clubs = playerClubsDetails
                .Select(c => new PlayerClub(c, playerClubs))
                .ToList();
            Clue = p.Clue;
            Country = (Countries)p.CountryId;
            Position = (Positions)p.PositionId;
            YearOfBirth = p.YearOfBirth;
        }
    }
}
