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
    public void From_CarriesThePendingDeletionRequest()
    {
        var requestDate = new System.DateTime(2026, 11, 20, 10, 0, 0);
        var user = UserDtoBuilder.Valid().WithId(5).WithLogin("lea").Build()
            with { DeletionRequested = true, DeletionRequestedDate = requestDate, DeletionReason = "je pars" };

        var row = UserAdminRow.From(user, "lea@exemple.fr");

        row.DeletionRequested.Should().BeTrue();
        row.DeletionRequestedDate.Should().Be(requestDate);
        row.DeletionReason.Should().Be("je pars");
    }

    [Fact]
    public void From_DoesNotFlagAnAccountWithoutDeletionRequest()
    {
        var user = UserDtoBuilder.Valid().WithId(5).WithLogin("lea").Build();

        UserAdminRow.From(user, "lea@exemple.fr").DeletionRequested.Should().BeFalse();
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