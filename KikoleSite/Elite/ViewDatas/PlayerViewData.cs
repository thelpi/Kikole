﻿using System;
using System.Collections.Generic;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.ViewDatas
{
    public class PlayerViewData
    {
        public static readonly int[] RankMilestones = new int[]
        {
            100, 50, 20, 10, 5, 3, 1
        };
        public static readonly int[] PointsMilestones = new int[]
        {
            1, 1000, 2000, 3000, 4000, 5000
        };

        public Game Game { get; set; }
        public string RealName { get; set; }
        public string SurName { get; set; }
        public string Color { get; set; }
        public string Country { get; set; }
        public DateTime? JoinDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public (int rank, int points, DateTime date)? BestPointsRank { get; set; }
        public (int rank, TimeSpan time, DateTime date)? BestTimeRank { get; set; }
        public IReadOnlyCollection<(DateTime date, int points)> RankingPointsMilestones { get; set; }
        public IReadOnlyCollection<(DateTime date, int rank)> RankingMilestones { get; set; }
        public IReadOnlyCollection<(DateTime date, int rank)> RankingHistory { get; set; }
        public PlayerWorldRecordsItemData WorldRecords { get; set; }
        public IReadOnlyDictionary<Engine, PlayerWorldRecordsItemData> EngineWorldRecords { get; set; }
    }
}
