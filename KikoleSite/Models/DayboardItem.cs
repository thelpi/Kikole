using System;

namespace KikoleSite.Models
{
    public abstract class DayboardItem
    {
        public DateTime Date { get; set; }
        public ulong UserId { get; set; }
        public string UserName { get; set; }
    }
}
