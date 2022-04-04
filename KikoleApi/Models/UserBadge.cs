using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class UserBadge : Badge
    {
        public DateTime GetDate { get; }

        internal UserBadge(BadgeDto badgeDto, UserBadgeDto userBadgeDto, int usersCount, string description)
            : base(badgeDto, usersCount, description)
        {
            GetDate = userBadgeDto.GetDate;
        }
    }
}
