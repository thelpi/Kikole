using System;
using KikoleSite;
using Moq;

namespace KikoleSiteUnitTests;

/// <summary>
/// Calendrier de jeu figé pour les tests. Les dates étaient auparavant lues sur une
/// constante statique globale : chaque test peut désormais s'ancrer sur les siennes.
/// </summary>
internal static class TestCalendar
{
    /// <summary>La vraie date d'ouverture du jeu, choisie pour rester parlante.</summary>
    internal static readonly DateTime FirstDate = new(2022, 3, 3);

    internal static readonly DateTime HiddenDate = FirstDate.AddDays(-1);

    internal static readonly DateTime FirstMonth = new(FirstDate.Year, FirstDate.Month, 1);

    internal static Mock<IGameCalendar> Mock()
    {
        var mock = new Mock<IGameCalendar>();
        mock.Setup(_ => _.FirstDate).Returns(FirstDate);
        mock.Setup(_ => _.HiddenDate).Returns(HiddenDate);
        mock.Setup(_ => _.FirstMonth).Returns(FirstMonth);
        return mock;
    }
}
