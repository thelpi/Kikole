﻿using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Interfaces
{
    public interface IBadgeRepository
    {
        Task<IReadOnlyCollection<BadgeDto>> GetBadgesAsync();

        Task InsertUserBadgeAsync(UserBadgeDto userBadge);

        Task<IReadOnlyCollection<UserBadgeDto>> GetUsersWithBadge(ulong badgeId);
    }
}