using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Leaderboard
    {
        public string Login { get; }

        public uint TotalPoints { get; }

        public TimeSpan BestTime { get; }

        public int SuccessCount { get; }

        internal Leaderboard(
            IGrouping<(string key, bool isIp), LeaderboardDto> userDtos,
            IReadOnlyCollection<UserDto> users)
        {
            Login = userDtos.Key.isIp
                ? userDtos.Key.key
                : users.Single(p => p.Id == ulong.Parse(userDtos.Key.key)).Login;
            SuccessCount = userDtos.Count();
            TotalPoints = (uint)userDtos.Sum(dto => dto.Points);
            BestTime = new TimeSpan(0, userDtos.Min(dto => dto.Time), 0);
        }
    }
}
