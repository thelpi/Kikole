using System;

namespace KikoleSite;

/// <summary>
/// Game calendar implementation.
/// </summary>
/// <seealso cref="IGameCalendar"/>
public class GameCalendar : IGameCalendar
{
    private DateOnly? _hiddenDate;

    /// <inheritdoc />
    public DateOnly HiddenDate => _hiddenDate
        ?? throw new InvalidOperationException("Le calendrier du jeu n'a pas ete amorce.");

    /// <inheritdoc />
    public DateOnly FirstDate => HiddenDate.AddDays(1);

    /// <inheritdoc />
    public DateOnly FirstMonth => new(FirstDate.Year, FirstDate.Month, 1);

    /// <summary>
    /// Fixe l'origine du calendrier. Réservé à <see cref="GameCalendarLoader"/>, qui
    /// l'appelle une fois au démarrage.
    /// </summary>
    /// <param name="hiddenDate">Date de la journée cachée.</param>
    internal void Initialize(DateOnly hiddenDate)
    {
        if (_hiddenDate.HasValue)
            throw new InvalidOperationException("Le calendrier du jeu a deja ete amorce.");

        _hiddenDate = hiddenDate;
    }
}
