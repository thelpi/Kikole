using FluentAssertions;
using KikoleSite.ViewModels;
using Xunit;

namespace KikoleSiteUnitTests.ViewModels;

public class UserAdminRowTests
{
    [Fact]
    public void From_FlagsAnAccountWhoseEmailWasNeverConfirmed()
    {
        var user = UserDtoBuilder.Valid().WithId(5).WithLogin("lea").WithEmailConfirmed(false).Build();

        UserAdminRow.From(user, "lea@exemple.fr").IsEmailUnconfirmed.Should().BeTrue();
    }

    [Fact]
    public void From_DoesNotFlagAConfirmedAccount()
    {
        var user = UserDtoBuilder.Valid().WithId(5).WithLogin("lea").WithEmailConfirmed().Build();

        UserAdminRow.From(user, "lea@exemple.fr").IsEmailUnconfirmed.Should().BeFalse();
    }

    [Fact]
    public void From_KeepsTheDisabledAndConfirmationFlagsIndependent()
    {
        var user = UserDtoBuilder.Valid().WithId(5).WithLogin("lea").WithDisabled().WithEmailConfirmed(false).Build();

        var row = UserAdminRow.From(user, "lea@exemple.fr");

        row.IsDisabled.Should().BeTrue();
        row.IsEmailUnconfirmed.Should().BeTrue();
    }
}