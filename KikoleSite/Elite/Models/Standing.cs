using System;
using System.Collections.Generic;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;

namespace KikoleSite.Elite.Models
{
    public class Standing : Ranking
    {
        private DateTime _startDate;
        private DateTime? _endDate;
        private readonly List<long> _times;

        public Stage Stage { get; internal set; }
        public Level Level { get; internal set; }
        public DateTime StartDate { get { return _startDate; } internal set { _startDate = value.Date; } }
        public DateTime? EndDate { get { return _endDate; } internal set { _endDate = value?.Date; } }
        public Player Author { get; internal set; }
        public Player Slayer { get; internal set; }
        public IReadOnlyCollection<long> Times => _times;
        public int? Days { get; private set; }
        public int DaysBefore => (int)Math.Floor((StartDate - Stage.GetGame().GetEliteFirstDate()).TotalDays);

        internal Standing(long time)
        {
            _times = new List<long>
            {
                time
            };
        }

        internal void AddTime(long time)
        {
            if (!_times.Contains(time))
                _times.Add(time);
        }

        public Standing WithDays(DateTime dateIfNull)
        {
            Days = (int)(EndDate.GetValueOrDefault(dateIfNull) - StartDate).TotalDays;
            return this;
        }

        public override string ToString()
        {
            var datas = new[]
            {
                $"{Stage} - {Level}",
                Author.ToString(Stage.GetGame()),
                $"{Days} {(Days <= 1 ? "day" : "days")}",
                $"From {StartDate:yyyy-MM-dd} {(EndDate.HasValue ? $"to {EndDate.Value:yyyy-MM-dd}" : "(ongoing)")}"
            };
            return string.Join('\n', datas);
        }
    }
}
