using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Api;

namespace KikoleSite
{
    public interface IApiProvider
    {
        #region user accounts

        Task<string> CreateAccountAsync(string login, string password, string question, string answer);

        Task<(bool, string)> LoginAsync(string login, string password);

        Task<bool> IsPowerUserAsync(string authToken);

        Task<bool> IsAdminUserAsync(string authToken);

        Task<string> ChangePasswordAsync(string authToken, string currentPassword, string newPassword);

        Task<string> ChangeQAndAAsync(string authToken, string question, string answer);

        Task<string> ResetPasswordAsync(string login, string answer, string newPassword);

        Task<(bool, string)> GetLoginQuestionAsync(string login);

        Task<IReadOnlyCollection<User>> GetActiveUsersAsync();

        #endregion user accounts

        #region stats, badges and leaderboard

        Task<IReadOnlyCollection<User>> GetUsersWithProposalAsync(DateTime date);

        Task<IReadOnlyCollection<Leader>> GetLeadersAsync(LeaderSort leaderSort, DateTime? minimalDate, DateTime? maximalDate, bool includePvp);

        Task<IReadOnlyCollection<Leader>> GetDayLeadersAsync(DateTime day, LeaderSort sort);

        Task<UserStats> GetUserStatsAsync(ulong id);

        Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(ulong userId, string authToken);

        Task<IReadOnlyCollection<Badge>> GetBadgesAsync();

        Task<IReadOnlyCollection<string>> GetUserKnownPlayersAsync(string authToken);

        Task<Awards> GetMonthlyAwardsAsync(int year, int month);

        #endregion stats, badges and leaderboard

        #region player creation

        Task<string> CreateClubAsync(string name, IReadOnlyList<string> allowedNames, string authToken);

        Task<string> CreatePlayerAsync(PlayerRequest player, string authToken);

        Task<IReadOnlyCollection<Player>> GetPlayerSubmissionsAsync(string authToken);

        Task<string> ValidatePlayerSubmissionAsync(PlayerSubmissionValidationRequest request, string authToken);

        #endregion player creation

        #region site management

        Task<IReadOnlyDictionary<ulong, string>> GetCountriesAsync();

        Task<IReadOnlyCollection<Club>> GetClubsAsync(bool resetCache = false);

        Task<string> GetCurrentMessageAsync();

        Task<ProposalChart> GetProposalChartAsync();

        #endregion site management

        #region main game

        Task<ProposalResponse> SubmitProposalAsync(string value, int daysBeforeNow, ProposalType proposalType, string authToken);

        Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(DateTime proposalDate, string authToken);

        Task<string> GetClueAsync(DateTime proposalDate, bool isEasy);

        Task<PlayerCreator> IsPlayerOfTheDayUser(DateTime proposalDate, string authToken);
        
        #endregion main game

        #region challenges
        
        Task<string> CreateChallengeAsync(ulong guestUserId, byte pointsRate, string authToken);
        
        Task<string> RespondToChallengeAsync(ulong id, bool isAccepted, string authToken);
        
        Task<IReadOnlyCollection<Challenge>> GetChallengesWaitingForResponseAsync(string authToken);
        
        Task<IReadOnlyCollection<Challenge>> GetRequestedChallengesAsync(string authToken);
        
        Task<IReadOnlyCollection<Challenge>> GetAcceptedChallengesAsync(string authToken);

        Task<IReadOnlyCollection<Challenge>> GetChallengesHistoryAsync(DateTime? fromDate, DateTime? toDate, string authToken);

        #endregion challenges
    }
}
