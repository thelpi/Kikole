using System;
using System.Threading.Tasks;
using KikoleSite;
using KikoleSite.Configuration;
using KikoleSite.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services;

public class IpRetentionServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 4, 12, 0, 0);

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IProposalRepository> _proposalRepository = new();
    private readonly Mock<IClock> _clock = new();

    private IpRetentionService CreateService(int ipMonths)
    {
        _clock.Setup(_ => _.Now).Returns(Now);

        return new IpRetentionService(
            _userRepository.Object,
            _proposalRepository.Object,
            _clock.Object,
            Options.Create(new RetentionOptions { IpMonths = ipMonths }),
            NullLogger<IpRetentionService>.Instance);
    }

    [Fact]
    public async Task PurgeAsync_ErasesIpsOlderThanTheConfiguredNumberOfMonths()
    {
        var service = CreateService(ipMonths: 12);

        await service.PurgeAsync();

        var cutoff = new DateTime(2025, 10, 4, 12, 0, 0);
        _userRepository.Verify(_ => _.PurgeIpAddressesAsync(cutoff), Times.Once);
        _proposalRepository.Verify(_ => _.ClearIpAddressesAsync(cutoff), Times.Once);
    }

    [Fact]
    public async Task PurgeAsync_UsesTheConfiguredDuration()
    {
        var service = CreateService(ipMonths: 6);

        await service.PurgeAsync();

        var cutoff = new DateTime(2026, 4, 4, 12, 0, 0);
        _userRepository.Verify(_ => _.PurgeIpAddressesAsync(cutoff), Times.Once);
        _proposalRepository.Verify(_ => _.ClearIpAddressesAsync(cutoff), Times.Once);
    }
}
