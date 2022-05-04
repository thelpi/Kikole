using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Helpers;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models
{
    public class Player : PlayerCreator
    {
        public ulong Id { get; }

        public IReadOnlyCollection<PlayerClub> Clubs { get; }

        public IReadOnlyCollection<string> AllowedNames { get; }

        public ushort YearOfBirth { get; }

        public Countries Country { get; }

        public DateTime? ProposalDate { get; }

        public string Clue { get; }

        public string EasyClue { get; }

        public Positions Position { get; }

        public DateTime? RejectDate { get; }

        internal Player(PlayerFullDto p, IEnumerable<UserDto> users)
            : base(users.Single(u => u.Id == p.Player.CreationUserId), p.Player)
        {
            Id = p.Player.Id;
            ProposalDate = p.Player.ProposalDate;
            RejectDate = p.Player.RejectDate;
            AllowedNames = p.Player.AllowedNames.Disjoin();
            Clubs = p.PlayerClubs
                .Select(c => new PlayerClub(c, p.Clubs))
                .ToList();
            Clue = p.Player.Clue;
            EasyClue = p.Player.EasyClue;
            Country = (Countries)p.Player.CountryId;
            Position = (Positions)p.Player.PositionId;
            YearOfBirth = p.Player.YearOfBirth;
        }
    }
}
