using System;
using System.Collections.Generic;
using KikoleSite.Enums;
using KikoleSite.Extensions;

namespace KikoleSite.Models
{
    public class Standing : Ranking
    {
        private DateTime _startDate;
        private DateTime? _endDate;
        private readonly List<int> _times;

        public Stage Stage { get; internal set; }
        public Level Level { get; internal set; }
        public DateTime StartDate { get => _startDate; internal set => _startDate = value.Date; }
        public DateTime? EndDate { get => _endDate; internal set => _endDate = value?.Date; }
        public Player Author { get; internal set; }
        public Player Slayer { get; internal set; }
        public IReadOnlyCollection<int> Times => _times;
        public int? Days { get; private set; }
        public int DaysBefore => (int)Math.Floor((StartDate - Stage.GetGame().GetEliteFirstDate()).TotalDays);
        public (DateTime, DateTime?) Period => (StartDate, EndDate);

        internal Standing(int time)
        {
            _times = new List<int>
            {
                time
            };
        }

        internal void AddTime(int time)
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
