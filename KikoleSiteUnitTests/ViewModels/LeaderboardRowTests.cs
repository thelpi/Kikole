using System;
using FluentAssertions;
using KikoleSite.Models;
using KikoleSite.ViewModels;
using Xunit;

namespace KikoleSiteUnitTests.ViewModels;

/// <summary>
/// Le formatage d'affichage du classement vit dans les ViewModels, pas dans les modeles
/// (<see cref="LeaderboardItem"/>, <see cref="DayboardLeaderItem"/> n'ont aucune notion de
/// presentation).
/// </summary>
public class LeaderboardRowTests
{
    private static LeaderboardItem Item(double percentage = 0, double? rarity = null) => new()
    {
        UserId = 1,
        UserName = "joueur",
        BadgePercentage = percentage,
        AverageBadgeRarity = rarity
    };

    [Theory]
    [InlineData(0, "0%")]
    [InlineData(45.4, "45%")]
    [InlineData(45.6, "46%")]
    [InlineData(100, "100%")]
    public void BadgePercentageIsRoundedAndSuffixedWithAPercentSign(double percentage, string expected)
    {
        LeaderboardRow.From(Item(percentage)).BadgePercentageString.Should().Be(expected);
    }

    [Fact]
    public void AverageRarityIsRoundedAndSuffixedWithAPercentSign()
    {
        LeaderboardRow.From(Item(rarity: 91.67)).AverageBadgeRarityString.Should().Be("92%");
    }

    [Fact]
    public void AverageRarityIsADashWhenTheUserHasNoBadge()
    {
        LeaderboardRow.From(Item(rarity: null)).AverageBadgeRarityString.Should().Be("-");
    }

    [Fact]
    public void BestTimeIsFormattedAndTheRawFieldsAreCarriedOver()
    {
        var item = new LeaderboardItem
        {
            UserId = 7,
            UserName = "joueur",
            Rank = 3,
            Points = 800,
            BestTime = new TimeSpan(1, 30, 0),
            KikolesFound = 4,
            KikolesAttempted = 5,
            KikolesProposed = 2,
            BadgesFound = 6,
            BadgesMissing = 27
        };

        var row = LeaderboardRow.From(item);

        row.BestTimeString.Should().Be("01:30");
        row.Rank.Should().Be(3);
        row.UserId.Should().Be(7UL);
        row.Points.Should().Be(800);
        row.KikolesFound.Should().Be(4);
        row.KikolesAttempted.Should().Be(5);
        row.KikolesProposed.Should().Be(2);
        row.BadgesFound.Should().Be(6);
        row.BadgesMissing.Should().Be(27);
    }

    [Fact]
    public void DayboardLeaderTimeIsFormatted()
    {
        var row = DayboardLeaderRow.From(new DayboardLeaderItem
        {
            UserId = 2,
            UserName = "joueur",
            Time = new TimeSpan(0, 12, 0),
            Points = 750,
            IsCreator = false,
            Rank = 1
        });

        row.TimeString.Should().Be("00:12");
        row.Points.Should().Be(750);
        row.Rank.Should().Be(1);
    }
}
