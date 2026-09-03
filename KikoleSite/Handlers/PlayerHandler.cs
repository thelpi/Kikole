using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using KikoleSite.Repositories;

namespace KikoleSite.Handlers;

/// <summary>
/// Player handler implementation.
/// </summary>
/// <seealso cref="IPlayerHandler"/>
public class PlayerHandler : IPlayerHandler
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IClubRepository _clubRepository;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
    /// <param name="clubRepository">Instance of <see cref="IClubRepository"/>.</param>
    public PlayerHandler(IPlayerRepository playerRepository,
        IClubRepository clubRepository)
    {
        _playerRepository = playerRepository;
        _clubRepository = clubRepository;
    }

    /// <inheritdoc />
    public async Task<PlayerFullDto> GetPlayerOfTheDayFullInfoAsync(DateTime date)
    {
        var p = await _playerRepository
            .GetPlayerOfTheDayAsync(date)
            ?? throw new InvalidOperationException($"Aucun joueur n'est programme pour le {date:yyyy-MM-dd}.");

        return await GetPlayerFullInfoAsync(p);
    }

    /// <inheritdoc />
    public async Task<PlayerFullDto> GetPlayerFullInfoAsync(PlayerDto p)
    {
        var playerClubs = await _playerRepository
            .GetPlayerClubsAsync(p.Id);

        List<ulong> distinctClubIds = [.. playerClubs.Select(pc => pc.ClubId).Distinct()];

        var clubs = await _clubRepository
            .GetClubsByIdsAsync(distinctClubIds);

        if (clubs.Count != distinctClubIds.Count)
        {
            var foundClubIds = clubs.Select(c => c.Id).ToHashSet();
            var missingClubIds = distinctClubIds.Where(id => !foundClubIds.Contains(id));
            throw new InvalidOperationException($"Club(s) introuvable(s) dans la carriere du joueur {p.Id} : {string.Join(", ", missingClubIds)}.");
        }

        return new PlayerFullDto
        {
            Clubs = [.. clubs],
            Player = p,
            PlayerClubs = playerClubs
        };
    }
}
