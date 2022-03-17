using System;

namespace KikoleApi.Models.Dtos
{
    public class UserBadgeDto
    {
        public ulong UserId { get; set; }

        public ulong BadgeId { get; set; }

        public DateTime GetDate { get; set; }
    }
}
