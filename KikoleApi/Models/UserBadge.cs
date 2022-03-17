using System;

namespace KikoleApi.Models
{
    public class UserBadge
    {
        public Badges Badge { get; set; }

        public DateTime GetDate { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ulong Users { get; set; }
    }
}
