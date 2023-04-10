﻿using System;
using System.Threading.Tasks;
using KikoleSite.Enums;
using KikoleSite.Models.Integration;

namespace KikoleSite.Providers
{
    public interface IIntegrationProvider
    {
        Task<RefreshEntriesResult> RefreshAllEntriesAsync(Game game);

        Task<RefreshEntriesResult> RefreshPlayerEntriesAsync(long playerId);

        Task<RefreshEntriesResult> RefreshEntriesToDateAsync(DateTime stopAt);

        Task<RefreshPlayersResult> RefreshPlayersAsync(bool addTimesForNewPlayers, bool refreshExistingPlayers);
    }
}
