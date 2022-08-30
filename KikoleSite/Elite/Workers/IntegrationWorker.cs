using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KikoleSite.Elite.Loggers;
using KikoleSite.Elite.Providers;

namespace KikoleSite.Elite.Workers
{
    public class IntegrationWorker : TimedHostedService
    {
        private readonly FileLogger _logger;
        private readonly Api.Interfaces.IClock _clock;
        private readonly IIntegrationProvider _integrationProvider;

        public IntegrationWorker(
            FileLogger logger,
            Api.Interfaces.IClock clock,
            IIntegrationProvider integrationProvider)
        {
            _logger = logger;
            _clock = clock;
            _integrationProvider = integrationProvider;
        }

        protected override TimeSpan DueTime =>
#if DEBUG
            // Now
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
        }
    }
}
