using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api;

namespace KikoleSite
{
    public interface IApiProvider
    {
        Task<string> CreateAccountAsync(string login, string password, string question, string answer);

        Task<ProposalResponse> SubmitProposalAsync(DateTime proposalDate, string value, int daysBefore, ProposalType proposalType, string authToken);

        Task<(bool, string)> LoginAsync(string login, string password);

        Task<IReadOnlyDictionary<ulong, string>> GetCountriesAsync(ulong languageId);

        Task<ProposalChart> GetProposalChartAsync();

        Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(DateTime proposalDate, string authToken);

        Task<IReadOnlyCollection<Leader>> GetLeadersAsync(LeaderSort leaderSort, DateTime? minimalDate, DateTime? maximalDate);

        Task<string> GetClueAsync(DateTime proposalDate);

        Task<IReadOnlyCollection<Club>> GetClubsAsync(bool resetCache = false);

        Task<IReadOnlyCollection<Leader>> GetDayLeadersAsync(DateTime day, LeaderSort sort);

        Task<UserStats> GetUserStatsAsync(ulong id);

        Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(ulong userId);

        Task<IReadOnlyCollection<Badge>> GetBadgesAsync();

        Task<string> CreateClubAsync(string name, IReadOnlyList<string> allowedNames, string authToken);

        Task<string> CreatePlayerAsync(PlayerRequest player, string authToken);

        Task<IReadOnlyCollection<string>> GetUserKnownPlayersAsync(string authToken);

        Task<string> GetCurrentMessageAsync();

        Task<bool> IsPowerUserAsync(string authToken);

        Task<bool> IsAdminUserAsync(string authToken);

        Task<string> ChangePasswordAsync(string authToken, string currentPassword, string newPassword);

        Task<string> ChangeQAndAAsync(string authToken, string question, string answer);

        Task<string> ResetPasswordAsync(string login, string answer, string newPassword);

        Task<(bool, string)> GetLoginQuestionAsync(string login);

        Task<PlayerCreator> IsPlayerOfTheDayUser(DateTime proposalDate, string authToken);

        Task<IReadOnlyCollection<Player>> GetPlayerSubmissionsAsync(string authToken);

        Task<string> ValidatePlayerSubmissionAsync(PlayerSubmissionValidationRequest request, string authToken);
    }
}
