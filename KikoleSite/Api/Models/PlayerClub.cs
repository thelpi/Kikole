﻿using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Models
{
    public class PlayerClub
    {
        public byte HistoryPosition { get; }

        public string Name { get; }

        public bool IsLoan { get; }

        internal PlayerClub(PlayerClubDto playerClub, IEnumerable<ClubDto> clubs)
        {
            Name = clubs.Single(c => c.Id == playerClub.ClubId).Name;
            HistoryPosition = playerClub.HistoryPosition;
            IsLoan = playerClub.IsLoan > 0;
        }
    }
}
