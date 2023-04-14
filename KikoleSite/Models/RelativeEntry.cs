using System;
using System.Collections.Generic;
using KikoleSite.Dtos;
using KikoleSite.Enums;

namespace KikoleSite.Models
{
    public class RelativeEntry
    {
        private readonly List<uint> _dupesOtherPlayers;

        public Stage Stage { get; }
        public Level Level { get; }
        public Player Player { get; }
        public DateTime Date { get; }
        public TimeSpan Time { get; }

        public int RelativeDifficulty { get; private set; }

        public int Count => 1 + _dupesOtherPlayers.Count;

        internal RelativeEntry(PlayerDto player, EntryDto entry)
        {
            Player = new Player(player);
            Date = entry.Date.Value;
            Time = new TimeSpan(0, 0, entry.Time);
            Stage = entry.Stage;
            Level = entry.Level;
            _dupesOtherPlayers = new List<uint>(5);
        }

        internal RelativeEntry AddDupeOrBetter(uint playerId)
        {
            if (!_dupesOtherPlayers.Contains(playerId))
                _dupesOtherPlayers.Add(playerId);
            return this;
        }

        internal RelativeEntry WithRelativeDifficulty(DateTime date)
        {
            RelativeDifficulty = (int)Math.Round((date.Date - Date).TotalDays / Count);
            return this;
        }
    }
}
