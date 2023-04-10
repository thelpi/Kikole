using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace KikoleSite.Workers
{
    public abstract class TimedHostedService : IHostedService, IDisposable
    {
        private Timer _timer;
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        protected abstract TimeSpan DueTime { get; }
        protected abstract TimeSpan Period { get; }
        protected abstract TaskStackBehavior StackBehavior { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(ExecuteTask, null, DueTime, Period);
            return Task.CompletedTask;
        }

        private void ExecuteTask(object state)
        {
            switch (StackBehavior)
            {
                case TaskStackBehavior.Parallel:
                    _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
                    break;
                case TaskStackBehavior.Skip:
                    if (_executingTask == null || _executingTask.IsCompleted)
                        _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
                    break;
                case TaskStackBehavior.Wait:
                    _executingTask?.Wait();
                    _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
                    break;
            }
        }

        private async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        {
            await RunJobAsync(stoppingToken).ConfigureAwait(false);
        }

        protected abstract Task RunJobAsync(CancellationToken stoppingToken);

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            if (_executingTask != null)
            {
                try
                {
                    _stoppingCts.Cancel();
                }
                finally
                {
                    await Task
                        .WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken))
                        .ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            _stoppingCts.Cancel();
            _timer?.Dispose();
        }
    }
}
