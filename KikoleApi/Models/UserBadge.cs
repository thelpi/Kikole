using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class UserBadge : Badge
    {
        public DateTime GetDate { get; }

        internal UserBadge(BadgeDto badgeDto, UserBadgeDto userBadgeDto, int usersCount)
            : base(badgeDto, usersCount)
        {
            GetDate = userBadgeDto.GetDate;
        }
    }
}
