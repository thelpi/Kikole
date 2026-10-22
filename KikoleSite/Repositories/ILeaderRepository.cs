using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Repositories;

public interface ILeaderRepository
{
    /// <summary>
    /// Enregistre la victoire d'un utilisateur sur un jour. Renvoie 0 sans rien ecrire s'il
    /// en a deja une pour ce jour (deux requetes simultanees, ex. double clic).
    /// </summary>
    Task<ulong> CreateLeaderAsync(LeaderDto request);

    Task<IReadOnlyCollection<LeaderDto>> GetLeadersAsync(DateOnly? minimalDate, DateOnly? maximalDate, bool onTimeOnly);

    Task<IReadOnlyCollection<LeaderDto>> GetUserLeadersAsync(DateOnly? minimalDate, DateOnly? maximalDate, bool onTimeOnly, ulong userId);

    Task<IReadOnlyCollection<LeaderDto>> GetLeadersAtDateAsync(DateOnly date, bool onTimeOnly);
}
