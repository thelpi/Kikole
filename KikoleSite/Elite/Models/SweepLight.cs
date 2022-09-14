using System;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Models
{
    public class SweepLight
    {
        public Stage Stage { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
        public int Days { get; set; }
        public long PlayerId { get; set; }
        public bool Untied { get; set; }
        public Engine? Engine { get; set; }
    }
}
