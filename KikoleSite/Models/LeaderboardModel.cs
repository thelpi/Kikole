﻿using System.Collections.Generic;

namespace KikoleSite.Models
{
    public class LeaderboardModel
    {
        public string MinimalDate { get; set; }

        public Api.LeaderSort SortType { get; set; }

        public IReadOnlyCollection<Api.Leader> Leaders { get; set; }

        public IReadOnlyCollection<Api.Leader> TodayLeaders { get; set; }
    }
}