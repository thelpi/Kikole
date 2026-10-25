using FluentAssertions;
using KikoleSite.Helpers;
using Xunit;

namespace KikoleSiteUnitTests.Helpers;

public class BadgeRuleSqlTests
{
    [Theory]
    [InlineData("SELECT user_id, badge_date FROM regle")]
    [InlineData("  select user_id, badge_date from regle where user_id = @userId")]
    [InlineData("SELECT user_id, MIN(badge_date) AS badge_date FROM regle GROUP BY user_id")]
    public void IsAllowed_AcceptsASingleSelect(string sql)
    {
        BadgeRuleSql.IsAllowed(sql).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("DELETE FROM user_badges")]
    [InlineData("UPDATE users SET login = 'x'")]
    [InlineData("SELECT 1; DELETE FROM users")]
    [InlineData("SELECT user_id FROM regle;")]
    [InlineData("SELECT * FROM users INTO OUTFILE '/tmp/x'")]
    [InlineData("select 1 into @v")]
    public void IsAllowed_RefusesAnythingButASingleReadOnlySelect(string? sql)
    {
        BadgeRuleSql.IsAllowed(sql).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_DoesNotMistakeAColumnNameForTheIntoKeyword()
    {
        BadgeRuleSql.IsAllowed("SELECT user_id, intolerance FROM regle").Should().BeTrue();
    }
}