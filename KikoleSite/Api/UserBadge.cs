using System;

namespace KikoleSite.Api
{
    public class UserBadge
    {
        public DateTime GetDate { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ulong Users { get; set; }
    }
}
