using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Models
{
    public class Wr : WrBase
    {
        private readonly List<(Player, DateTime, Engine)> _holders = new List<(Player, DateTime, Engine)>();

        public Player Player => _holders[0].Item1;
        public DateTime Date => _holders[0].Item2;
        public Engine Engine => _holders[0].Item3;
        public Player UntiedSlayPlayer => _holders.Count > 1 ? _holders[1].Item1 : default;
        public DateTime? UntiedSlayDate => _holders.Count > 1 ? _holders[1].Item2 : default(DateTime?);
        public IReadOnlyCollection<(Player, DateTime, Engine)> Holders => _holders;
        public DateTime? SlayDate { get; private set; }
        public Player SlayPlayer { get; private set; }

        internal Wr(Stage stage, Level level, long time, PlayerDto player, DateTime date, Engine engine)
            : base(stage, level, time)
        {
            _holders.Add((new Player(player), date.Date, engine));
        }

        internal void AddHolder(PlayerDto player, DateTime date, Engine engine)
        {
            // Avoid duplicate of same player on multiple engines
            if (_holders.Any(h => h.Item1.Id == player.Id)) return;

            _holders.Add((new Player(player), date.Date, engine));
        }

        internal void AddSlayer(PlayerDto player, DateTime date)
        {
            SlayPlayer = new Player(player);
            SlayDate = date.Date;
        }

        internal bool CheckAmbiguousHolders(int i)
        {
            return _holders.Count > i && _holders[i - 1].Item2 == _holders[i].Item2;
        }

        public int GetDaysBeforeSlay(DateTime date, bool untied)
        {
            var endDate = date.Date;

            if (untied && UntiedSlayDate.HasValue && UntiedSlayDate < date)
                endDate = UntiedSlayDate.Value;
            else if (!untied && SlayDate.HasValue && SlayDate < date)
                endDate = SlayDate.Value;

            return (int)Math.Floor((endDate - Date).TotalDays);
        }

        // TODO: ugly
        internal WrBase ToBase()
        {
            return new WrBase(Stage, Level, Time);
        }
    }
}
