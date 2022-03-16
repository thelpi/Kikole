using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Leader
    {
        public ulong Id { get; }

        public string Login { get; }

        public uint TotalPoints { get; }

        public TimeSpan BestTime { get; }

        public int SuccessCount { get; }

        public int Position { get; internal set; }

        internal Leader(IGrouping<ulong, LeaderDto> userDtos,
            IReadOnlyCollection<UserDto> users)
        {
            var user = users.Single(p => p.Id == userDtos.Key);
            Login = user.Login;
            SuccessCount = userDtos.Count();
            TotalPoints = (uint)userDtos.Sum(dto => dto.Points);
            BestTime = new TimeSpan(0, userDtos.Min(dto => dto.Time), 0);
            Id = user.Id;
        }
    }
}
