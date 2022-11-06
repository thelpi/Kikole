using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
{
    public class Player : PlayerCreator
    {
        public ulong Id { get; }

        public IReadOnlyCollection<PlayerClub> Clubs { get; }

        public IReadOnlyCollection<PlayerFederation> Federations { get; }

        public ushort YearOfBirth { get; }

        public Continents Continent { get; }

        public Countries Country { get; }

        public DateTime? ProposalDate { get; }

        public string Clue { get; }

        public string EasyClue { get; }

        public Positions Position { get; }

        public DateTime? RejectDate { get; }

        internal Player(PlayerFullDto p, IEnumerable<UserDto> users, IEnumerable<Federation> federations)
            : base(users.Single(u => u.Id == p.Player.CreationUserId), p.Player)
        {
            Id = p.Player.Id;
            ProposalDate = p.Player.ProposalDate;
            RejectDate = p.Player.RejectDate;
            Clubs = p.PlayerClubs
                .Select(c => new PlayerClub(c, p.Clubs))
                .ToList();
            Federations = p.PlayerFederations
                .Select(f => new PlayerFederation(f, federations))
                .ToList();
            Clue = p.Player.Clue;
            EasyClue = p.Player.EasyClue;
            Continent = (Continents)p.Player.ContinentId;
            Country = (Countries)p.Player.CountryId;
            Position = (Positions)p.Player.PositionId;
            YearOfBirth = p.Player.YearOfBirth;
        }
    }
}
