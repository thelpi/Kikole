using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class UserBadge : Badge
    {
        public DateTime GetDate { get; set; }

        internal UserBadge(BadgeDto badgeDto, UserBadgeDto userBadgeDto)
            : base(badgeDto)
        {
            GetDate = userBadgeDto.GetDate;
        }
    }
}
