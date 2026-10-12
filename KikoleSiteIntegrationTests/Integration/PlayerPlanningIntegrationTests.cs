using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteIntegrationTests.Integration;

/// <summary>
/// Caracterise <see cref="PlayerRepository.ReassignPublicationDatesAsync"/> : les dates sont
/// reecrites ensemble, et un kikole non programme apres aujourd'hui annule tout.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class PlayerPlanningIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public PlayerPlanningIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<ulong> CreateAsync(string login, DateOnly date)
    {
        var userRepository = new UserRepository(_fixture.Configuration, _fixture.Clock);
        var playerRepository = new PlayerRepository(_fixture.Configuration, _fixture.Clock);
        var creatorId = await userRepository.CreateUserAsync(UserDtoBuilder.Valid().WithLogin(login).Build());

        return await playerRepository.CreatePlayerAsync(
            PlayerDtoBuilder.Valid().WithName(login).WithAllowedNames(login).WithCreator(creatorId).WithPublicationDate(date).Build());
    }

    [Fact]
    public async Task ReassignPublicationDatesAsync_RewritesAllTheDatesTogether()
    {
        var repository = new PlayerRepository(_fixture.Configuration, _fixture.Clock);
        var day = _fixture.Clock.Today.AddDays(800);
        var (a, b, c) = (await CreateAsync("integration_planning_a", day), await CreateAsync("integration_planning_b", day.AddDays(1)), await CreateAsync("integration_planning_c", day.AddDays(5)));

        var done = await repository.ReassignPublicationDatesAsync([(c, day), (a, day.AddDays(1)), (b, day.AddDays(2))]);

        done.Should().BeTrue();
        (await repository.GetPlayerByIdAsync(c))!.PublicationDate.Should().Be(day);
        (await repository.GetPlayerByIdAsync(a))!.PublicationDate.Should().Be(day.AddDays(1));
        (await repository.GetPlayerByIdAsync(b))!.PublicationDate.Should().Be(day.AddDays(2));
    }

    [Fact]
    public async Task ReassignPublicationDatesAsync_WhenAPlayerIsNotScheduledAfterToday_ChangesNothing()
    {
        var repository = new PlayerRepository(_fixture.Configuration, _fixture.Clock);
        var day = _fixture.Clock.Today.AddDays(900);
        var future = await CreateAsync("integration_planning_future", day);
        var today = await CreateAsync("integration_planning_today", _fixture.Clock.Today);

        var done = await repository.ReassignPublicationDatesAsync([(future, day.AddDays(3)), (today, day.AddDays(4))]);

        done.Should().BeFalse();
        (await repository.GetPlayerByIdAsync(future))!.PublicationDate.Should().Be(day);
        (await repository.GetPlayerByIdAsync(today))!.PublicationDate.Should().Be(_fixture.Clock.Today);
    }
}