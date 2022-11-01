using System;
using KikoleSite.Models.Enums;

namespace KikoleSite.ViewModels
{
    public class UserDayItemModel
    {
        public DateTime Date { get; set; }
        public int PointsLost { get; set; }
        public int PointsRemaining { get; set; }
        public ProposalTypes Type { get; set; }
        public string Value { get; set; }
        public bool Success { get; set; }
    }
}
