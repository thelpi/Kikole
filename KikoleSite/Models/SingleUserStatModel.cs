using System;
using KikoleSite.Api;

namespace KikoleSite.Models
{
    public class SingleUserStatModel
    {
        public DateTime Date { get; set; }

        public string Answer { get; set; }

        public bool? Attempt { get; set; }

        public bool? Success { get; set; }

        public TimeSpan? Time { get; set; }

        public int? Points { get; set; }

        public int? TimePosition { get; set; }

        public int? PointsPosition { get; set; }

        public bool IsCreator { get; set; }

        public SingleUserStatModel(SingleUserStat apiStat, bool knowPlayer)
        {
            Answer = knowPlayer ? apiStat.Answer : "***";
            Date = apiStat.Date;
            Points = apiStat.Points;
            PointsPosition = apiStat.PointsPosition;
            Time = apiStat.Time;
            TimePosition = apiStat.TimePosition;
            Attempt = apiStat.Attempt;
            Success = apiStat.Time.HasValue;

            if (apiStat.Points.HasValue && !apiStat.Time.HasValue)
            {
                Attempt = null;
                Success = null;
                IsCreator = true;
            }
        }
    }
}
