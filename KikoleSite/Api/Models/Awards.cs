using System;
using System.Collections.Generic;
using KikoleSite.Api.Helpers;

namespace KikoleSite.Api.Models
{
    public class Awards
    {
        public IReadOnlyCollection<PointsAward> PointsAwards { get; set; }

        public IReadOnlyCollection<TimeAward> TimeAwards { get; set; }

        public IReadOnlyCollection<CountAward> CountAwards { get; set; }

        public IReadOnlyCollection<KikoleAward> HardestKikoles { get; set; }

        public IReadOnlyCollection<KikoleAward> EasiestKikoles { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }
    }

    public class BaseAward
    {
        public int Position { get; set; }

        public string Name { get; set; }
    }

    public class PointsAward : BaseAward
    {
        public int Points { get; set; }
    }

    public class TimeAward : BaseAward
    {
        internal TimeSpan Time { get; set; }

        public int TimeSec => Time.ToSeconds();

        public string PlayerName { get; set; }
    }

    public class CountAward : BaseAward
    {
        public int Count { get; set; }
    }

    public class KikoleAward : BaseAward
    {
        public int AveragePoints { get; set; }
    }
}
