using System;

namespace KikoleSite.Models.Statistics
{
    public class PlayerStatistics
    {
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Creator { get; set; }
        public int AveragePointsSameDay { get; set; }
        public int TriesCountSameDay { get; set; }
        public int SuccessesCountSameDay { get; set; }
        public int AveragePointsTotal { get; set; }
        public int TriesCountTotal { get; set; }
        public int SuccessesCountTotal { get; set; }
        public int BestTime { get; set; }

        public int DaysBefore => (int)(DateTime.Now - Date).TotalDays;
    }
}
