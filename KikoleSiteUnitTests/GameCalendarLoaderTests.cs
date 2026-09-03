using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Repositories;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests;

/// <summary>
/// L'origine du calendrier est deduite des donnees : le tout premier joueur publie est
/// la journee cachee.
/// </summary>
public class GameCalendarLoaderTests
{
    private static readonly DateTime Earliest = new(2022, 3, 2);

    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly GameCalendar _calendar = new();
    private readonly GameCalendarLoader _loader;

    public GameCalendarLoaderTests()
    {
        _loader = new GameCalendarLoader(_playerRepository.Object, _calendar);
    }

    private void EarliestPlayerIs(DateTime? date)
    {
        _playerRepository.Setup(_ => _.GetEarliestPlayerDateAsync()).ReturnsAsync(date);
    }

    [Fact]
    public async Task TheHiddenDayIsTheEarliestPublishedPlayer()
    {
        EarliestPlayerIs(Earliest);

        await _loader.StartAsync(CancellationToken.None);

        _calendar.HiddenDate.Should().Be(Earliest);
        _calendar.FirstDate.Should().Be(Earliest.AddDays(1));
    }

    [Fact]
    public async Task TheRepositoryIsReadOnce()
    {
        EarliestPlayerIs(Earliest);

        await _loader.StartAsync(CancellationToken.None);

        _ = _calendar.FirstDate;
        _ = _calendar.FirstMonth;

        _playerRepository.Verify(_ => _.GetEarliestPlayerDateAsync(), Times.Once);
    }

    /// <summary>
    /// Sans joueur, le calendrier n'a pas d'origine : plutot que de servir des dates
    /// inventees, l'application refuse de demarrer.
    /// </summary>
    [Fact]
    public async Task WhenThereIsNoPlayerAtAll_StartupFails()
    {
        EarliestPlayerIs(null);

        Func<Task> act = () => _loader.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*joueur*");
    }
}
