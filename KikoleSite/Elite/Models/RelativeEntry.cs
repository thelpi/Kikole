using System;
using System.Collections.Generic;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Models
{
    public class RelativeEntry
    {
        private readonly List<long> _dupesOtherPlayers;

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
            Time = new TimeSpan(0, 0, (int)entry.Time);
            Stage = entry.Stage;
            Level = entry.Level;
            _dupesOtherPlayers = new List<long>();
        }

        internal RelativeEntry AddDupeOrBetter(long playerId)
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
