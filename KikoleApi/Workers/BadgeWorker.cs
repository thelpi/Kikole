using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
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

        public BadgeWorker(IConfiguration configuration,
            IClock clock,
            IBadgeRepository badgeRepository,
            ILeaderRepository leaderRepository)
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
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _timer.Start();
            return Task.CompletedTask;
        }

        private async Task DoWorkAsync()
        {
            var yesterday = _clock.Now.AddDays(-1).Date;

            var leaders = await _leaderRepository
                .GetLeadersAtDateAsync(yesterday)
                .ConfigureAwait(false);

            foreach (var badge in BadgeHelper.LeadersBasedBadgeCondition.Keys)
            {
                var usersWithbadge = await _badgeRepository
                    .GetUsersWithBadgeAsync((ulong)badge)
                    .ConfigureAwait(false);

                foreach (var leader in leaders.Where(l =>
                    BadgeHelper.LeadersBasedBadgeCondition[badge](l, leaders)
                    && !usersWithbadge.Any(u => u.UserId == l.UserId)))
                {
                    await _badgeRepository
                        .InsertUserBadgeAsync(new UserBadgeDto
                        {
                            GetDate = yesterday,
                            BadgeId = (ulong)badge,
                            UserId = leader.UserId
                        })
                        .ConfigureAwait(false);
                }
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
