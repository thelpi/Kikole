using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Timer = System.Timers.Timer;

namespace KikoleApi.Workers
{
    public class BadgeWorker : IHostedService
    {
        private readonly int _workerEnableHour;
        private readonly Timer _timer;
        private readonly IClock _clock;
        private readonly IBadgeRepository _badgeRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IPlayerRepository _playerRepository;

        public BadgeWorker(IConfiguration configuration,
            IClock clock,
            IBadgeRepository badgeRepository,
            ILeaderRepository leaderRepository,
            IPlayerRepository playerRepository)
        {
            _timer = new Timer(1000 * 60 * 60);
            _timer.Elapsed += (s, e) =>
            {
                if (_workerEnableHour == _clock.Now.Hour)
                {
                    Task.Run(DoWorkAsync);
                }
            };
            _workerEnableHour = configuration.GetValue<int>("WorkerEnableHour");
            _clock = clock;
            _badgeRepository = badgeRepository;
            _leaderRepository = leaderRepository;
            _playerRepository = playerRepository;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _timer.Start();
            return Task.CompletedTask;
        }

        private async Task DoWorkAsync()
        {
            var badges = Enum.GetValues(typeof(Badges)).Cast<Badges>();

            var yesterday = _clock.Now.AddDays(-1).Date;

            var leaders = await _leaderRepository
                .GetLeadersAtDateAsync(yesterday)
                .ConfigureAwait(false);

            var playerOfTheDay = await _playerRepository
                .GetPlayerOfTheDayAsync(yesterday)
                .ConfigureAwait(false);

            // Special badge
            if (playerOfTheDay.YearOfBirth < 1970)
            {
                var usersWithbadge = await _badgeRepository
                    .GetUsersWithBadge((ulong)Badges.Archaeology)
                    .ConfigureAwait(false);

                foreach (var leader in leaders)
                {
                    await CheckUserForBadgeAsync(
                            yesterday, Badges.Archaeology, usersWithbadge, leader.UserId)
                        .ConfigureAwait(false);
                }
            }

            var oldLeaders = new List<IReadOnlyCollection<LeaderDto>>();
            for (var i = 1; i <= 6; i++)
            {
                var leadersBefore = await _leaderRepository
                    .GetLeadersAtDateAsync(yesterday.AddDays(-i))
                    .ConfigureAwait(false);
                oldLeaders.Add(leadersBefore);
            }

            var badgeCondition = new Dictionary<Badges, Func<LeaderDto, bool>>
            {
                {
                    Badges.CacaCaféClopeKikolé,
                    l => new TimeSpan(0, l.Time, 0).Hours >= 5 && new TimeSpan(0, l.Time, 0).Hours < 8
                },
                {
                    Badges.HalfwayToTheTop,
                    l => l.Points >= 500
                },
                {
                    Badges.ItsOver900,
                    l => l.Points >= 900
                },
                {
                    Badges.SavedByTheBell,
                    l => new TimeSpan(0, l.Time, 0).Hours == 23
                },
                {
                    Badges.StayUpLate,
                    l => new TimeSpan(0, l.Time, 0).Hours < 2
                },
                {
                    Badges.YourActualFirstSuccess,
                    l => l.Points > 0
                },
                {
                    Badges.YourFirstSuccess,
                    l => true
                },
                {
                    Badges.OverTheTopPart1,
                    l => l.Time == leaders.Min(_ => _.Time)
                },
                {
                    Badges.OverTheTopPart2,
                    l => l.Points == leaders.Min(_ => _.Points)
                },
                {
                    Badges.ThreeInARow,
                    l => oldLeaders.Take(2).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                },
                {
                    Badges.AWeekInARow,
                    l => oldLeaders.All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                }
            };

            foreach (var badge in badges)
            {
                var usersWithbadge = await _badgeRepository
                    .GetUsersWithBadge((ulong)badge)
                    .ConfigureAwait(false);

                if (badgeCondition.ContainsKey(badge))
                {
                    foreach (var leader in leaders.Where(l => badgeCondition[badge](l)))
                    {
                        await CheckUserForBadgeAsync(
                                yesterday, badge, usersWithbadge, leader.UserId)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task CheckUserForBadgeAsync(DateTime yesterday, Badges badge, IEnumerable<UserBadgeDto> usersWithbadge, ulong userId)
        {
            if (!usersWithbadge.Any(u => u.UserId == userId))
            {
                await _badgeRepository
                    .InsertUserBadgeAsync(new UserBadgeDto
                    {
                        GetDate = yesterday,
                        BadgeId = (ulong)badge,
                        UserId = userId
                    })
                    .ConfigureAwait(false);
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _timer.Stop();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
