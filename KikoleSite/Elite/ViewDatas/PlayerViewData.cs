using System;
using System.Collections.Generic;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;

namespace KikoleSite.Elite.ViewDatas
{
    public class PlayerViewData
    {
        public Game Game { get; set; }
        public long Id { get; set; }
        public string RealName { get; set; }
        public string SurName { get; set; }
        public string Color { get; set; }
        public string Country { get; set; }
        public StandingItemData FirstWorldRecord { get; set; }
        public DateTime? JoinDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public PlayerWorldRecordsItemData WorldRecords { get; set; }
        public IReadOnlyCollection<SweepItemData> Three00PtsSweeps { get; set; }
        public IReadOnlyCollection<SweepItemData> UntiedSweeps { get; set; }

        public DateTime DefaultFirstDate => Game.GetEliteFirstDate();
        public DateTime DefaultLastDate => DateTime.Today;
    }
}
