using System;

namespace KikoleSite.Api.Models
{
    public abstract class DayboardItem
    {
        public DateTime Date { get; set; }
        public ulong UserId { get; set; }
        public string UserName { get; set; }
    }
}
