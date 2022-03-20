﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api;

namespace KikoleSite
{
    public interface IApiProvider
    {
        Task<string> CreateAccountAsync(string login, string password, string question, string answer);

        Task<ProposalResponse> SubmitProposalAsync(DateTime proposalDate, string value, int daysBefore, ProposalType proposalType, string authToken, int sourcePoints);

        Task<(bool, string)> LoginAsync(string login, string password);

        Task<IReadOnlyDictionary<ulong, string>> GetCountriesAsync(ulong languageId);

        Task<ProposalChart> GetProposalChartAsync();

        Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync( DateTime proposalDate, string authToken);

        Task<IReadOnlyCollection<Leader>> GetLeadersAsync(LeaderSort leaderSort, DateTime? minimalDate);

        Task<string> GetClueAsync(DateTime proposalDate);

        Task<IReadOnlyCollection<Club>> GetClubsAsync(bool resetCache = false);

        Task<IReadOnlyCollection<Leader>> GetTodayLeadersAsync();

        Task<UserStats> GetUserStats(ulong id);

        Task<IReadOnlyCollection<UserBadge>> GetUserBadges(ulong userId);

        Task<string> CreateClubAsync(string name, IReadOnlyList<string> allowedNames, string authToken);

        Task<string> CreatePlayerAsync(PlayerRequest player, string authToken);
    }
}
