using System;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;

namespace KikoleSite.Elite.Models
{
    public class Player
    {
        // Virtual date where submitted times should have a date, except for newcomers.
        internal static readonly DateTime LastEmptyDate = new DateTime(2013, 1, 1);
        // Default player's hexadecimal color.
        internal const string DefaultPlayerHexColor = "000000";

        public long Id { get; }
        public string RealName { get; }
        public string SurName { get; }
        public ControlStyle? ControlStyle { get; }
        public string Color { get; }

        internal Player(PlayerDto dto)
        {
            Id = dto.Id;
            RealName = dto.RealName;
            SurName = dto.SurName;
            ControlStyle = ModelExtensions.ToControlStyle(dto.ControlStyle);
            Color = dto.Color;
        }

        public string ToString(Game game)
        {
            return game == Game.PerfectDark
                ? SurName
                : RealName;
        }
    }
}
