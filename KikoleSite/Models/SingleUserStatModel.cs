using KikoleSite.Api;

namespace KikoleSite.Models
{
    public class SingleUserStatModel
    {
        public string Date { get; set; }

        public string Answer { get; set; }

        public string Attempt { get; set; }

        public string Success { get; set; }

        public string Time { get; set; }

        public string Points { get; set; }

        public string TimePosition { get; set; }

        public string PointsPosition { get; set; }

        public SingleUserStatModel(SingleUserStat apiStat, bool knowPlayer)
        {
            Answer = knowPlayer ? apiStat.Answer : "***";
            Attempt = apiStat.Attempt.ToYesNo();
            Date = apiStat.Date.ToNaString();
            Points = apiStat.Points.ToNaString();
            PointsPosition = apiStat.PointsPosition.ToNaString();
            Success = apiStat.Time.HasValue.ToYesNo();
            Time = apiStat.Time.ToNaString();
            TimePosition = apiStat.TimePosition.ToNaString();
        }
    }
}
