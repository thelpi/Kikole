using FluentAssertions;
using KikoleSite.Configuration;
using KikoleSite.Controllers;
using Microsoft.Extensions.Options;
using Xunit;

namespace KikoleSiteUnitTests.Controllers;

public class SeoControllerTests
{
    private static SeoController Controller(string baseUrl) =>
        new(Options.Create(new SeoOptions { CanonicalBaseUrl = baseUrl }));

    [Fact]
    public void Robots_PointsToTheSitemapOfTheCanonicalAddress_AndHidesPrivateAreas()
    {
        var result = Controller("https://www.kikole.fr/").Robots();

        result.ContentType.Should().StartWith("text/plain");
        result.Content.Should().Contain("Sitemap: https://www.kikole.fr/sitemap.xml");
        result.Content.Should().Contain("Disallow: /Admin").And.Contain("Disallow: /Account");
    }

    [Fact]
    public void Sitemap_ListsTheIndexablePagesOnTheCanonicalAddress()
    {
        var result = Controller("https://www.kikole.fr").Sitemap();

        result.ContentType.Should().StartWith("application/xml");
        result.Content.Should().Contain("<loc>https://www.kikole.fr/</loc>")
            .And.Contain("<loc>https://www.kikole.fr/Leaderboard</loc>")
            .And.Contain("<loc>https://www.kikole.fr/Home/Legal</loc>")
            .And.NotContain("Admin");
    }
}
