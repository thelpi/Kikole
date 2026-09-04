using System;
using System.Threading;
using System.Threading.Tasks;
using KikoleSite.Repositories;
using Microsoft.Extensions.Hosting;

namespace KikoleSite;

/// <summary>
/// Amorce le calendrier du jeu à partir des données, avant la première requête.
/// </summary>
/// <remarks>
/// C'est ici que vit la seule dépendance à un dépôt : <see cref="GameCalendar"/> lui-même
/// n'en a aucune, ce qui lui permet d'être injecté à n'importe quelle couche. Personne
/// n'injecte ce chargeur, l'hôte s'en charge.
/// </remarks>
public class GameCalendarLoader : IHostedService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly GameCalendar _gameCalendar;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
    /// <param name="gameCalendar">Calendrier à amorcer.</param>
    public GameCalendarLoader(IPlayerRepository playerRepository, GameCalendar gameCalendar)
    {
        _playerRepository = playerRepository;
        _gameCalendar = gameCalendar;
    }

    /// <summary>
    /// L'hôte attend cette méthode avant d'accepter des requêtes.
    /// </summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Rien.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // le tout premier joueur publie est la journee cachee ; le jeu commence le lendemain
        var earliest = await _playerRepository.GetEarliestPlayerDateAsync()
            ?? throw new InvalidOperationException(
                "Aucun joueur en base : le calendrier du jeu ne peut pas etre determine.");

        _gameCalendar.Initialize(earliest);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
