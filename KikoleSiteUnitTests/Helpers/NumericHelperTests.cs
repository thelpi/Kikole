using FluentAssertions;
using KikoleSite.Helpers;
using Xunit;

namespace KikoleSiteUnitTests.Helpers;

public class NumericHelperTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(5, 10, 50)]
    [InlineData(10, 10, 100)]
    [InlineData(1, 3, 33)]
    [InlineData(2, 3, 67)]      // arrondi au superieur
    [InlineData(15, 10, 150)]   // pas de plafonnement a 100
    public void ToPercentRate_ReturnsRoundedPercentage(int numerator, int denominator, int expected)
    {
        numerator.ToPercentRate(denominator).Should().Be(expected);
    }

    [Fact]
    public void ToPercentRate_WhenDenominatorIsZero_ReturnsZeroInsteadOfThrowing()
    {
        5.ToPercentRate(0).Should().Be(0);
    }
}
