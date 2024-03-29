﻿using System;
using System.Threading.Tasks;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Models.Integration;

namespace KikoleSite.Elite.Providers
{
    public interface IIntegrationProvider
    {
        Task<RefreshEntriesResult> RefreshAllEntriesAsync(Game game);

        Task<RefreshEntriesResult> RefreshPlayerEntriesAsync(long playerId);

        Task<RefreshEntriesResult> RefreshEntriesToDateAsync(DateTime stopAt);

        Task<RefreshPlayersResult> RefreshPlayersAsync(bool addTimesForNewPlayers, bool refreshExistingPlayers);
    }
}
