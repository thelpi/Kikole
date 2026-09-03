using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Handlers;
using KikoleSite.Models.Dtos;
using KikoleSite.Repositories;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Handlers;

public class PlayerHandlerTests
{
    private readonly PlayerHandler _handler;
    private readonly Mock<IPlayerRepository> _playerRepository;
    private readonly Mock<IClubRepository> _clubRepository;

    public PlayerHandlerTests()
    {
        _playerRepository = new Mock<IPlayerRepository>();
        _clubRepository = new Mock<IClubRepository>();
        _handler = new PlayerHandler(_playerRepository.Object, _clubRepository.Object);
    }

    private static PlayerDto Player => PlayerDtoBuilder.Valid().WithId(1).WithName("Zinédine Zidane").Build();

    private void SetupCareer(params (ulong clubId, string name)[] clubs)
    {
        var playerClubs = new List<PlayerClubDto>();
        byte position = 1;
        foreach (var (clubId, name) in clubs)
        {
            playerClubs.Add(new PlayerClubDto { PlayerId = 1, ClubId = clubId, HistoryPosition = position++ });
            _clubRepository
                .Setup(_ => _.GetClubAsync(clubId))
                .ReturnsAsync(ClubDtoBuilder.Valid().WithId(clubId).WithName(name).Build());
        }

        _playerRepository
            .Setup(_ => _.GetPlayerClubsAsync(1))
            .ReturnsAsync(playerClubs);
    }

    [Fact]
    public async Task GetPlayerFullInfoAsync_AssemblesPlayerCareerAndClubs()
    {
        SetupCareer((2, "AS Cannes"), (3, "Juventus"), (4, "Real Madrid"));

        var result = await _handler.GetPlayerFullInfoAsync(Player);

        result.Player.Name.Should().Be("Zinédine Zidane");
        result.PlayerClubs.Should().HaveCount(3);
        result.Clubs.Should().HaveCount(3);
        result.Clubs.Should().Contain(c => c.Name == "Real Madrid");
    }

    [Fact]
    public async Task GetPlayerFullInfoAsync_WhenNoCareer_ReturnsEmptyCollections()
    {
        SetupCareer();

        var result = await _handler.GetPlayerFullInfoAsync(Player);

        result.PlayerClubs.Should().BeEmpty();
        result.Clubs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlayerFullInfoAsync_AClubPlayedTwiceIsFetchedOnce()
    {
        // un joueur peut revenir dans un club (retour de pret, second passage) :
        // le handler doit dedoublonner avant d'interroger le depot
        _playerRepository
            .Setup(_ => _.GetPlayerClubsAsync(1))
            .ReturnsAsync(new List<PlayerClubDto>
            {
                new PlayerClubDto { PlayerId = 1, ClubId = 2, HistoryPosition = 1 },
                new PlayerClubDto { PlayerId = 1, ClubId = 3, HistoryPosition = 2 },
                new PlayerClubDto { PlayerId = 1, ClubId = 2, HistoryPosition = 3 }
            });
        _clubRepository.Setup(_ => _.GetClubAsync(2)).ReturnsAsync(ClubDtoBuilder.Valid().WithId(2).WithName("Juventus").Build());
        _clubRepository.Setup(_ => _.GetClubAsync(3)).ReturnsAsync(ClubDtoBuilder.Valid().WithId(3).WithName("Inter Milan").Build());

        var result = await _handler.GetPlayerFullInfoAsync(Player);

        result.PlayerClubs.Should().HaveCount(3);
        result.Clubs.Should().HaveCount(2);
        _clubRepository.Verify(_ => _.GetClubAsync(2), Times.Once);
    }

    [Fact]
    public async Task GetPlayerOfTheDayFullInfoAsync_LooksUpTheDayThenAssembles()
    {
        var date = new DateTime(2026, 9, 2);

        _playerRepository
            .Setup(_ => _.GetPlayerOfTheDayAsync(date))
            .ReturnsAsync(Player);
        SetupCareer((4, "Real Madrid"));

        var result = await _handler.GetPlayerOfTheDayFullInfoAsync(date);

        result.Player.Id.Should().Be(1);
        result.Clubs.Should().ContainSingle(c => c.Name == "Real Madrid");
        _playerRepository.Verify(_ => _.GetPlayerOfTheDayAsync(date), Times.Once);
    }

    [Fact]
    public async Task GetPlayerOfTheDayFullInfoAsync_WhenNoPlayerForThatDay_SaysWhichDayIsMissing()
    {
        // un joueur par jour est une invariante du jeu : son absence est une faute
        // d'administration, signalee par une exception qui nomme la date en cause
        _playerRepository
            .Setup(_ => _.GetPlayerOfTheDayAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((PlayerDto?)null);

        Func<Task> act = () => _handler.GetPlayerOfTheDayFullInfoAsync(new DateTime(2026, 9, 2));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*2026-09-02*");
    }
}
