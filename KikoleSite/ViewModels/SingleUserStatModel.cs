using System;
using KikoleSite.Models;

namespace KikoleSite.ViewModels
{
    public class SingleUserStatModel
    {
        public DateTime Date { get; }

        public string Answer { get; }

        public bool? AttemptDayOne { get; }

        public bool? Attempt { get; }

        public bool? SuccessDayOne { get; }

        public bool? Success { get; }

        public TimeSpan? Time { get; }

        public int? Points { get; }

        public int? TimePosition { get; }

        public int? PointsPosition { get; }

        public bool IsCreator { get; }

        public int DaysBefore { get; }

        public SingleUserStatModel(DailyUserStat apiStat, IClock clock)
        {
            Answer = apiStat.Answer;
            Date = apiStat.Date;
            DaysBefore = (int)(clock.Now - Date).TotalDays;
            Points = apiStat.Points;
            PointsPosition = apiStat.PointsPosition;
            Time = apiStat.Time;
            TimePosition = apiStat.TimePosition;
            Attempt = apiStat.Attempt;
            AttemptDayOne = apiStat.AttemptDayOne;
            Success = apiStat.Success;
            SuccessDayOne = apiStat.SuccessDayOne;
            IsCreator = apiStat.Points.HasValue && !apiStat.Time.HasValue;

            if (IsCreator)
            {
                Attempt = null;
                AttemptDayOne = null;
                Success = null;
                SuccessDayOne = null;
            }
        }
    }
}
