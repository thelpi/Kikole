using FluentAssertions;
using KikoleSite.Helpers;
using Xunit;

namespace KikoleSiteUnitTests.Helpers;

public class SeoPagesTests
{
    [Theory]
    [InlineData("/", false, true)]
    [InlineData("/Leaderboard", false, true)]
    [InlineData("/leaderboard/", false, true)]
    [InlineData("/Home/Legal", false, true)]
    [InlineData("/", true, false)]
    [InlineData("/Leaderboard", true, false)]
    [InlineData("/Account", false, false)]
    [InlineData("/Admin/Users", false, false)]
    [InlineData("/Home/Contact", false, false)]
    [InlineData("", false, false)]
    [InlineData(null, false, false)]
    public void IsIndexable_OnlyForPublicPagesWithoutQueryString(string? path, bool hasQueryString, bool expected)
    {
        SeoPages.IsIndexable(path, hasQueryString).Should().Be(expected);
    }
}
