using System;
using System.Threading;
using System.Threading.Tasks;
using KikoleSite.Configuration;
using KikoleSite.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KikoleSite;

/// <summary>
/// Efface les adresses IP au-delà de leur durée de conservation (<see cref="RetentionOptions.IpMonths"/>) :
/// au démarrage, puis une fois par jour.
/// </summary>
public class IpRetentionService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly IProposalRepository _proposalRepository;
    private readonly IClock _clock;
    private readonly RetentionOptions _options;
    private readonly ILogger<IpRetentionService> _logger;

    public IpRetentionService(
        IUserRepository userRepository,
        IProposalRepository proposalRepository,
        IClock clock,
        IOptions<RetentionOptions> options,
        ILogger<IpRetentionService> logger)
    {
        _userRepository = userRepository;
        _proposalRepository = proposalRepository;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PurgeAsync()
    {
        var cutoff = _clock.Now.AddMonths(-_options.IpMonths);

        await _userRepository.PurgeIpAddressesAsync(cutoff);
        await _proposalRepository.ClearIpAddressesAsync(cutoff);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeAsync();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Échec de la purge des adresses IP.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}