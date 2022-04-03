using System;
using KikoleSite.Api;

namespace KikoleSite.Models
{
    public class SingleUserStatModel
    {
        public DateTime Date { get; set; }

        public string Answer { get; set; }

        public bool Attempt { get; set; }

        public bool Success { get; set; }

        public TimeSpan? Time { get; set; }

        public int? Points { get; set; }

        public int? TimePosition { get; set; }

        public int? PointsPosition { get; set; }

        public SingleUserStatModel(SingleUserStat apiStat, bool knowPlayer)
        {
            Answer = knowPlayer ? apiStat.Answer : "***";
            Attempt = apiStat.Attempt;
            Date = apiStat.Date;
            Points = apiStat.Points;
            PointsPosition = apiStat.PointsPosition;
            Success = apiStat.Time.HasValue;
            Time = apiStat.Time;
            TimePosition = apiStat.TimePosition;
        }
    }
}
