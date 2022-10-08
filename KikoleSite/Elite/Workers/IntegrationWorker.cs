using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KikoleSite.Elite.Loggers;
using KikoleSite.Elite.Providers;
using KikoleSite.Elite.Repositories;

namespace KikoleSite.Elite.Workers
{
    public class IntegrationWorker : TimedHostedService
    {
        private readonly FileLogger _logger;
        private readonly IClock _clock;
        private readonly IIntegrationProvider _integrationProvider;
        private readonly ICacheManager _cacheManager;

        public IntegrationWorker(
            FileLogger logger,
            IClock clock,
            IIntegrationProvider integrationProvider,
            ICacheManager cacheManager)
        {
            _logger = logger;
            _clock = clock;
            _integrationProvider = integrationProvider;
            _cacheManager = cacheManager;
        }

        protected override TimeSpan DueTime =>
#if DEBUG
            // Never
            TimeSpan.FromMilliseconds(-1);
#else
            // At next midnight
            TimeSpan.FromMilliseconds((int)Math.Ceiling((_clock.Tomorrow - _clock.Now).TotalMilliseconds));
#endif

        protected override TimeSpan Period => TimeSpan.FromDays(1);

        protected override TaskStackBehavior StackBehavior => TaskStackBehavior.Skip;

        // TODO: use the stoppingToken!
        protected override async Task RunJobAsync(CancellationToken stoppingToken)
        {
            await _cacheManager
                .ToggleCacheLockAsync(true)
                .ConfigureAwait(false);

            var isGlobalRefresh = _clock.Today.Day == 1;

            var refreshPlayersResult = await _integrationProvider
                .RefreshPlayersAsync(!isGlobalRefresh, isGlobalRefresh)
                .ConfigureAwait(false);

            _logger.Log(refreshPlayersResult.Errors?.ToArray());

            if (isGlobalRefresh)
            {
                var geRefreshEntriesResult = await _integrationProvider
                    .RefreshAllEntriesAsync(Enums.Game.GoldenEye)
                    .ConfigureAwait(false);

                var pdRefreshEntriesResult = await _integrationProvider
                    .RefreshAllEntriesAsync(Enums.Game.PerfectDark)
                    .ConfigureAwait(false);

                _logger.Log(geRefreshEntriesResult.Errors?.ToArray());
                _logger.Log(pdRefreshEntriesResult.Errors?.ToArray());
            }
            else
            {
                var refreshEntriesResult = await _integrationProvider
                    .RefreshEntriesToDateAsync(_clock.Today.Truncat(Enums.DateStep.Month))
                    .ConfigureAwait(false);

                _logger.Log(refreshEntriesResult.Errors?.ToArray());
            }

            await _cacheManager
                .ToggleCacheLockAsync(false)
                .ConfigureAwait(false);
        }
    }
}
