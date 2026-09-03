using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Models.Dtos;
using KikoleSite.Repositories;
using Xunit;

namespace KikoleSiteUnitTests.Integration;

/// <summary>
/// Caracterise <see cref="BadgeRepository.GetUsersOfTheDayWithBadgeAsync"/> : filtre les
/// detenteurs d'un badge sur une journee precise, pas juste tous les detenteurs. Sert de
/// filet avant de deplacer ce filtre en SQL (aujourd'hui charge tout l'historique du badge
/// et filtre en C#) — sur le chemin chaud (<c>HomeController</c> a chaque soumission
/// gagnante), contrairement a <c>BadgeService.ResetBadgesAsync</c> laisse tel quel.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class UsersOfTheDayWithBadgeIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public UsersOfTheDayWithBadgeIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUsersOfTheDayWithBadgeAsync_OnlyReturnsHoldersForThatDay()
    {
        var badgeRepository = new BadgeRepository(_fixture.Configuration, _fixture.Clock);

        const ulong badgeId = 1;
        var today = _fixture.Clock.Today;
        var yesterday = today.AddDays(-1);

        // joueur1 (id 2) a eu ce badge hier, joueur2 (id 3) l'a aujourd'hui
        await badgeRepository.InsertUserBadgeAsync(new UserBadgeDto { BadgeId = badgeId, UserId = 2, GetDate = yesterday });
        await badgeRepository.InsertUserBadgeAsync(new UserBadgeDto { BadgeId = badgeId, UserId = 3, GetDate = today });

        var holdersToday = await badgeRepository.GetUsersOfTheDayWithBadgeAsync(badgeId, today);

        holdersToday.Select(h => h.UserId).Should().BeEquivalentTo([3ul]);
    }
}
