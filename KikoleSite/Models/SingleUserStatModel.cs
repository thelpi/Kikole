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

        public SingleUserStatModel(Api.SingleUserStat apiStat)
        {
            Answer = "???"; // TODO !
            Attempt = apiStat.Attempt ? "Yes" : "No";
            Date = apiStat.Date.ToString("yyyy-MM-dd");
            Points = apiStat.Points?.ToString() ?? "N/A";
            PointsPosition = apiStat.PointsPosition?.ToString() ?? "N/A";
            Success = apiStat.Time.HasValue ? "Yes" : "No";
            Time = apiStat.Time?.ToString(@"hh\:mm") ?? "N/A";
            TimePosition = apiStat.TimePosition?.ToString() ?? "N/A";
        }
    }
}
