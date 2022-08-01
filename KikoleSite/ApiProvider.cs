using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Interfaces.Services;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;
using KikoleSite.Api.Models.Requests;
using KikoleSite.Api.Models.Statistics;
using Microsoft.Extensions.Localization;

namespace KikoleSite
{
    public class ApiProvider : IApiProvider
    {
        // minutes
        private const int DelayBetweenUserChecks = 10;

        private static ConcurrentDictionary<string, IReadOnlyDictionary<ulong, string>> _countriesCache;
        private static ProposalChart _proposalChartCache;
        private static IReadOnlyCollection<Club> _clubsCache;

        private readonly ConcurrentDictionary<ulong, DateTime> _usersCheckCache;
        private readonly IUserRepository _userRepository;
        private readonly ICrypter _crypter;
        private readonly IStringLocalizer<Translations> _resources;
        private readonly IInternationalRepository _internationalRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IClock _clock;
        private readonly IPlayerService _playerService;
        private readonly IClubRepository _clubRepository;
        private readonly IProposalService _proposalService;
        private readonly IBadgeService _badgeService;
        private readonly IChallengeService _challengeService;
        private readonly ILeaderService _leaderService;
        private readonly IStatisticService _statisticService;
        private readonly IDiscussionRepository _discussionRepository;

        public ApiProvider(IUserRepository userRepository,
            ICrypter crypter,
            IStringLocalizer<Translations> resources,
            IInternationalRepository internationalRepository,
            IMessageRepository messageRepository,
            IClock clock,
            IPlayerService playerService,
            IClubRepository clubRepository,
            IProposalService proposalService,
            IBadgeService badgeService,
            IChallengeService challengeService,
            ILeaderService leaderService,
            IStatisticService statisticService,
            IDiscussionRepository discussionRepository)
        {
            _userRepository = userRepository;
            _crypter = crypter;
            _resources = resources;
            _internationalRepository = internationalRepository;
            _messageRepository = messageRepository;
            _clock = clock;
            _playerService = playerService;
            _clubRepository = clubRepository;
            _proposalService = proposalService;
            _badgeService = badgeService;
            _challengeService = challengeService;
            _leaderService = leaderService;
            _statisticService = statisticService;
            _discussionRepository = discussionRepository;
            _usersCheckCache = new ConcurrentDictionary<ulong, DateTime>();
        }

        #region user accounts

        public async Task<IReadOnlyCollection<User>> GetActiveUsersAsync()
        {
            var users = await _userRepository
                .GetActiveUsersAsync()
                .ConfigureAwait(false);

            return users.Select(u => new User(u)).ToList();
        }

        public async Task<string> CreateAccountAsync(string login,
            string password, string question, string answer, string ip)
        {
            var request = new UserRequest
            {
                Login = login,
                Password = password,
                PasswordResetQuestion = question,
                PasswordResetAnswer = answer?.Trim(),
                Ip = ip
            };

            if (request == null)
                return string.Format(_resources["InvalidRequest"], "null");

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return string.Format(_resources["InvalidRequest"], validityRequest);

            var existingUser = await _userRepository
                .GetUserByLoginAsync(request.Login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser != null)
                return _resources["AlreadyExistsAccount"];

            var userId = await _userRepository
                .CreateUserAsync(request.ToDto(_crypter))
                .ConfigureAwait(false);

            if (userId == 0)
                return _resources["UserCreationFailure"];

            return null;
        }

        public async Task<(bool, string)> LoginAsync(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
                return (false, _resources["InvalidLogin"]);

            if (string.IsNullOrWhiteSpace(password))
                return (false, _resources["InvalidPassword"]);

            var existingUser = await _userRepository
                .GetUserByLoginAsync(login.Sanitize())
                .ConfigureAwait(false);

            if (existingUser == null)
                return (false, _resources["UserDoesNotExist"]);

            if (!_crypter.Encrypt(password).Equals(existingUser.Password))
                return (false, _resources["PasswordDoesNotMatch"]);

            var value = $"{existingUser.Id}_{existingUser.UserTypeId}";

            var token = $"{value}_{_crypter.Encrypt(value)}";

            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "fr"
                    ? "Echec de l'authentification : jeton invalide"
                    : "Authentication failed: invalid token");
            }

            return (true, token);
        }

        public async Task<bool> IsPowerUserAsync(string authToken)
        {
            return await IsTypeOfUserAsync(
                    authToken, UserTypes.PowerUser)
                .ConfigureAwait(false);
        }

        public async Task<bool> IsAdminUserAsync(string authToken)
        {
            return await IsTypeOfUserAsync(
                    authToken, UserTypes.Administrator)
                .ConfigureAwait(false);
        }

        public async Task<string> ChangePasswordAsync(string authToken,
            string currentPassword, string newPassword)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return _resources["InvalidUser"];

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return _resources["InvalidPassword"];

            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return _resources["UserDoesNotExist"];

            var success = await _userRepository
                .ResetUserKnownPasswordAsync(
                    user.Login,
                    _crypter.Encrypt(currentPassword),
                    _crypter.Encrypt(newPassword))
                .ConfigureAwait(false);

            if (!success)
                return _resources["ResetPasswordError"];

            return null;
        }

        public async Task<string> ChangeQAndAAsync(string authToken,
            string question, string answer)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return _resources["InvalidUser"];

            if (string.IsNullOrWhiteSpace(question)
                || string.IsNullOrWhiteSpace(answer))
                return _resources["InvalidQOrA"];

            await _userRepository
                .ResetUserQAndAAsync(userId, question, _crypter.Encrypt(answer))
                .ConfigureAwait(false);

            return null;
        }

        public async Task<string> ResetPasswordAsync(string login,
            string answer, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(login))
                return _resources["InvalidLogin"];

            if (string.IsNullOrWhiteSpace(answer))
                return _resources["InvalidQOrA"];

            if (string.IsNullOrWhiteSpace(newPassword))
                return _resources["InvalidPassword"];

            var response = await _userRepository
                .ResetUserUnknownPasswordAsync(
                    login,
                    _crypter.Encrypt(answer),
                    _crypter.Encrypt(newPassword))
                .ConfigureAwait(false);

            if (!response)
                return _resources["ResetPasswordError"];

            return null;
        }

        public async Task<(bool, string)> GetLoginQuestionAsync(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return (false, _resources["InvalidLogin"]);

            var user = await _userRepository
                .GetUserByLoginAsync(login)
                .ConfigureAwait(false);

            if (user == null)
                return (false, _resources["UserDoesNotExist"]);

            return (true, user.PasswordResetQuestion);
        }

        #endregion user accounts

        #region stats, badges and leaderboard

        public async Task<Leaderboard> GetLeaderboardAsync(LeaderSorts leaderSort, DateTime minimalDate, DateTime maximalDate)
        {
            return await _leaderService
                .GetLeaderboardAsync(minimalDate, maximalDate, leaderSort)
                .ConfigureAwait(false);
        }

        public async Task<Dayboard> GetDayboardAsync(DateTime day, DayLeaderSorts sort)
        {
            return await _leaderService
                .GetDayboardAsync(day, sort)
                .ConfigureAwait(false);
        }

        public async Task<UserStat> GetUserStatsAsync(ulong id)
        {
            if (id == 0)
                return null;

            var userStatistics = await _leaderService
                .GetUserStatisticsAsync(id)
                .ConfigureAwait(false);

            if (userStatistics == null)
                return null;

            return userStatistics;
        }

        public async Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(ulong userId, string authToken)
        {
            var connectedUserId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            var badgesFull = await _badgeService
                 .GetUserBadgesAsync(userId, connectedUserId, GetLanguage())
                 .ConfigureAwait(false);

            return badgesFull;
        }

        public async Task<IReadOnlyCollection<Badge>> GetBadgesAsync()
        {
            return await _badgeService
                .GetAllBadgesAsync(GetLanguage())
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<string>> GetUserKnownPlayersAsync(string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            return await _playerService
                .GetKnownPlayerNamesAsync(userId)
                .ConfigureAwait(false);
        }

        public async Task<PlayersDistribution> GetPlayersDistributionAsync(string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            return await _statisticService
                .GetPlayersDistributionAsync(userId, GetLanguage(), 25)
                .ConfigureAwait(false);
        }

        public async Task<ActiveUsers> GetStatisticActiveUsersAsync()
        {
            return await _statisticService
                .GetActiveUsersAsync()
                .ConfigureAwait(false);
        }

        #endregion stats, badges and leaderboard

        #region player creation

        public async Task<string> CreateClubAsync(string name,
            IReadOnlyList<string> allowedNames, string authToken)
        {
            var request = new ClubRequest
            {
                Name = name,
                AllowedNames = allowedNames
            };

            if (request == null)
                return string.Format(_resources["InvalidRequest"], "null");

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return string.Format(_resources["InvalidRequest"], validityRequest);

            var playerId = await _clubRepository
                .CreateClubAsync(request.ToDto())
                .ConfigureAwait(false);

            if (playerId == 0)
                return _resources["ClubCreationFailure"];

            return null;
        }

        public async Task<string> CreatePlayerAsync(PlayerRequest player, string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return _resources["InvalidUser"];

            if (player == null)
                return string.Format(_resources["InvalidRequest"], "null");

            var validityRequest = player.IsValid(_clock.Today, _resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return string.Format(_resources["InvalidRequest"], validityRequest);

            await _playerService
                .CreatePlayerAsync(player, userId)
                .ConfigureAwait(false);

            return null;
        }

        public async Task<IReadOnlyCollection<Player>> GetPlayerSubmissionsAsync(string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return null;

            var players = await _playerService
                .GetPlayerSubmissionsAsync()
                .ConfigureAwait(false);

            return players;
        }

        public async Task<string> ValidatePlayerSubmissionAsync(
            PlayerSubmissionValidationRequest request, string authToken)
        {
            var callerUserId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (request == null || callerUserId == 0)
                return string.Format(_resources["InvalidRequest"], "null");

            var validityCheck = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityCheck))
                return string.Format(_resources["InvalidRequest"], validityCheck);

            var (result, userId, badges) = await _playerService
                .ValidatePlayerSubmissionAsync(request)
                .ConfigureAwait(false);

            if (result == PlayerSubmissionErrors.PlayerNotFound)
                return _resources["PlayerDoesNotExist"];

            if (result == PlayerSubmissionErrors.PlayerAlreadyAcceptedOrRefused)
                return _resources["RejectAndProposalDateCombined"];

            foreach (var badge in badges)
            {
                await _badgeService
                    .AddBadgeToUserAsync(badge, userId)
                    .ConfigureAwait(false);
            }

            return null;
        }

        public async Task<string> UpdatePlayerCluesAsync(ulong playerId, string clueEn, string easyClueEn, string clueFr, string easyClueFr, string authToken)
        {
            var callerUserId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (callerUserId == 0)
                return string.Format(_resources["InvalidRequest"], "null");

            var isAdmin = await IsAdminUserAsync(authToken).ConfigureAwait(false);
            if (!isAdmin)
                return string.Format(_resources["InvalidRequest"], "null");

            await _playerService
                .UpdatePlayerCluesAsync(playerId, clueEn, easyClueEn,
                    new Dictionary<Languages, string> { { Languages.fr, clueFr } },
                    new Dictionary<Languages, string> { { Languages.fr, easyClueFr } })
                .ConfigureAwait(false);

            return null;
        }

        public async Task<(string clueEn, string clueFr, string easyClueEn, string easyClueFr, string error)> GetPlayerCluesAsync(ulong playerId, string authToken)
        {
            var callerUserId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);
            if (callerUserId == 0)
                return (null, null, null, null, "KO");

            var isAdmin = await IsAdminUserAsync(authToken).ConfigureAwait(false);
            if (!isAdmin)
                return (null, null, null, null, "KO");

            var clues = await _playerService
                .GetPlayerCluesAsync(playerId, new List<Languages> { Languages.en, Languages.fr })
                .ConfigureAwait(false);

            return (clues[Languages.en].clue, clues[Languages.fr].clue, clues[Languages.en].easyclue, clues[Languages.fr].easyclue, null);
        }

        #endregion player creation

        #region site management

        public async Task<IReadOnlyDictionary<ulong, string>> GetCountriesAsync()
        {
            if (_countriesCache?.ContainsKey(CultureInfo.CurrentCulture.TwoLetterISOLanguageName) != true)
            {
                var lng = GetLanguage();

                var countries = await _internationalRepository
                    .GetCountriesAsync((ulong)lng)
                    .ConfigureAwait(false);

                var apiCountries = countries.Select(c => new Country(c)).OrderBy(c => c.Name).ToList();

                var dict = apiCountries.ToDictionary(ac => (ulong)ac.Code, ac => ac.Name);

                _countriesCache ??= new ConcurrentDictionary<string, IReadOnlyDictionary<ulong, string>>();
                _countriesCache.AddOrUpdate(
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    dict,
                    (k, v) => dict);
            }

            return _countriesCache[CultureInfo.CurrentCulture.TwoLetterISOLanguageName];
        }

        public async Task<ProposalChart> GetProposalChartAsync()
        {
            if (_proposalChartCache == null)
            {
                ProposalChart.Default.FirstDate = await _playerService
                    .GetFirstSubmittedPlayerDateAsync(false)
                    .ConfigureAwait(false);

                _proposalChartCache = ProposalChart.Default;
            }

            return _proposalChartCache;
        }

        public async Task<IReadOnlyCollection<Club>> GetClubsAsync(bool resetCache = false)
        {
            if (_clubsCache == null || resetCache)
            {
                var clubs = await _clubRepository
                    .GetClubsAsync()
                    .ConfigureAwait(false);

                _clubsCache = clubs.Select(c => new Club(c)).OrderBy(c => c.Name).ToList();
            }

            return _clubsCache;
        }

        public async Task<string> GetCurrentMessageAsync()
        {
            var message = await _messageRepository
                .GetMessageAsync(_clock.Now)
                .ConfigureAwait(false);

            return message?.Message;
        }

        public async Task CreateMessageAsync(string message, DateTime? startDate, DateTime? endDate)
        {
            await _messageRepository
                .InsertMessageAsync(new Api.Models.Dtos.MessageDto
                {
                    DisplayTo = endDate,
                    DisplayFrom = startDate,
                    CreationDate = _clock.Now,
                    Message = message
                })
                .ConfigureAwait(false);
        }

        public async Task<string> CreateDiscussionAsync(string email, string message, string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return _resources["InvalidUser"];

            await _discussionRepository
                .CreateDiscussionAsync(new Api.Models.Dtos.DiscussionDto
                {
                    Email = email,
                    UserId = userId,
                    Message = message
                })
                .ConfigureAwait(false);

            return null;
        }

        public async Task ResetBadgesAsync()
        {
            await _badgeService
                .ResetBadgesAsync(GetLanguage())
                .ConfigureAwait(false);
        }

        public async Task ComputeMissingLeadersAsync()
        {
            await _leaderService
                .ComputeMissingLeadersAsync()
                .ConfigureAwait(false);
        }

        public async Task ReassignPlayersOfTheDayAsync()
        {
            await _playerService
                .ReassignPlayersOfTheDayAsync()
                .ConfigureAwait(false);
        }

        #endregion site management

        #region main game

        public async Task<ProposalResponse> SubmitProposalAsync(string value,
            uint daysBeforeNow,
            ProposalTypes proposalType,
            string authToken,
            string ip)
        {
            var request = BaseProposalRequest.Create(_clock.Now, value, daysBeforeNow, proposalType, ip);

            if (request == null)
                return null;

            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return null;

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return null;

            var firstDate = await _playerService
                .GetFirstSubmittedPlayerDateAsync(true)
                .ConfigureAwait(false);

            if (request.PlayerSubmissionDate < firstDate.Date || request.PlayerSubmissionDate > _clock.Today)
                return null;

            var pInfo = await _playerService
                .GetPlayerOfTheDayFullInfoAsync(request.PlayerSubmissionDate)
                .ConfigureAwait(false);

            var (response, proposalsAlready, leader) = await _proposalService
                .ManageProposalResponseAsync(request, userId, pInfo)
                .ConfigureAwait(false);

            if (leader != null)
            {
                var leaderBadges = await _badgeService
                    .PrepareNewLeaderBadgesAsync(leader, pInfo.Player, proposalsAlready, GetLanguage())
                    .ConfigureAwait(false);

                foreach (var b in leaderBadges)
                    response.AddBadge(b);
            }

            var proposalBadges = await _badgeService
                .PrepareNonLeaderBadgesAsync(userId, request, GetLanguage())
                .ConfigureAwait(false);

            foreach (var b in proposalBadges)
                response.AddBadge(b);

            return response;
        }

        public async Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(
            DateTime proposalDate, string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return null;

            var proposals = await _proposalService
                .GetProposalsAsync(proposalDate, userId)
                .ConfigureAwait(false);

            return proposals;
        }

        public async Task<string> GetClueAsync(DateTime proposalDate, bool isEasy)
        {
            var clue = await _playerService
                .GetPlayerClueAsync(proposalDate, isEasy, GetLanguage())
                .ConfigureAwait(false);

            return clue;
        }

        public async Task<PlayerCreator> IsPlayerOfTheDayUser(
            DateTime proposalDate, string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return null;

            var p = await _playerService
                .GetPlayerOfTheDayFromUserPovAsync(userId, proposalDate)
                .ConfigureAwait(false);

            return p;
        }

        #endregion main game

        #region challenges

        public async Task<string> CreateChallengeAsync(ulong guestUserId, byte pointsRate, string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return _resources["InvalidUser"];

            var request = new ChallengeRequest
            {
                GuestUserId = guestUserId,
                PointsRate = pointsRate
            };

            if (request == null)
                return string.Format(_resources["InvalidRequest"], "null");

            var validityRequest = request.IsValid(_resources);
            if (!string.IsNullOrWhiteSpace(validityRequest))
                return string.Format(_resources["InvalidRequest"], validityRequest);

            if (request.GuestUserId == userId)
                return _resources["CantChallengeYourself"];

            var response = await _challengeService
                .CreateChallengeAsync(request, userId)
                .ConfigureAwait(false);

            if (response == ChallengeResponseError.ChallengeHostIsInvalid)
                return _resources["ChallengeHostIsInvalid"];

            if (response == ChallengeResponseError.ChallengeCreatorIsAdmin)
                return _resources["ChallengeCreatorIsAdmin"];

            if (response == ChallengeResponseError.ChallengeOpponentIsInvalid)
                return _resources["ChallengeOpponentIsInvalid"];

            if (response == ChallengeResponseError.ChallengeOpponentIsAdmin)
                return _resources["ChallengeOpponentIsAdmin"];

            if (response == ChallengeResponseError.ChallengeAlreadyExist)
                return _resources["ChallengeAlreadyExist"];

            return null;
        }

        public async Task<string> RespondToChallengeAsync(ulong id, bool isAccepted, string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return _resources["InvalidUser"];

            if (id == 0)
                return _resources["InvalidChallengeId"];

            var response = await _challengeService
                .RespondToChallengeAsync(id, userId, isAccepted)
                .ConfigureAwait(false);

            if (response == ChallengeResponseError.ChallengeNotFound)
                return _resources["ChallengeNotFound"];

            if (response == ChallengeResponseError.CantAutoAcceptChallenge)
                return _resources["CantAutoAcceptChallenge"];

            if (response == ChallengeResponseError.BothAcceptedAndCancelledChallenge)
                return _resources["BothAcceptedAndCancelledChallenge"];

            if (response == ChallengeResponseError.ChallengeAlreadyAccepted)
                return _resources["ChallengeAlreadyAccepted"];

            if (response == ChallengeResponseError.ChallengeAlreadyAnswered)
                return _resources["ChallengeAlreadyAnswered"];

            if (response == ChallengeResponseError.InvalidOpponentAccount)
                return _resources["InvalidOpponentAccount"];

            await _badgeService
                .ManageChallengesBasedBadgesAsync(id)
                .ConfigureAwait(false);

            return null;
        }

        public async Task<IReadOnlyCollection<Challenge>> GetChallengesWaitingForResponseAsync(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken))
                return new List<Challenge>();

            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return new List<Challenge>();

            var challenges = await _challengeService
                .GetPendingChallengesAsync(userId)
                .ConfigureAwait(false);

            return challenges;
        }

        public async Task<IReadOnlyCollection<Challenge>> GetRequestedChallengesAsync(string authToken)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return new List<Challenge>();

            var challenges = await _challengeService
                .GetRequestedChallengesAsync(userId)
                .ConfigureAwait(false);

            return challenges;
        }

        public async Task<IReadOnlyCollection<Challenge>> GetAcceptedChallengesAsync(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken))
                return new List<Challenge>();

            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return new List<Challenge>();

            var challenges = await _challengeService
                .GetAcceptedChallengesAsync(userId)
                .ConfigureAwait(false);

            return challenges;
        }

        public async Task<IReadOnlyCollection<Challenge>> GetChallengesHistoryAsync(
            DateTime? fromDate, DateTime? toDate, string authToken)
        {
            var parameters = new List<(string, string)>();
            if (fromDate.HasValue)
                parameters.Add(("fromDate", fromDate.Value.ToString("yyyy-MM-dd")));
            if (toDate.HasValue)
                parameters.Add(("toDate", toDate.Value.ToString("yyyy-MM-dd")));

            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            if (userId == 0)
                return null;

            var debut = await _playerService
                .GetFirstSubmittedPlayerDateAsync(false)
                .ConfigureAwait(false);

            if (fromDate.HasValue && fromDate.Value.Date < debut.Date)
                return null;

            var yesterday = _clock.Now.AddDays(-1).Date;
            if (toDate.HasValue && toDate.Value.Date < yesterday)
                return null;

            if (toDate.HasValue && fromDate.HasValue && toDate.Value.Date < fromDate.Value.Date)
                return null;

            var challenges = await _challengeService
                .GetChallengesHistoryAsync(userId, fromDate?.Date ?? debut.Date, toDate?.Date ?? yesterday)
                .ConfigureAwait(false);

            return challenges;
        }

        #endregion challenges

        #region private generic methods

        private async Task<bool> IsTypeOfUserAsync(string authToken, UserTypes minimalType)
        {
            var userId = await ExtractUserIdFromTokenAsync(authToken).ConfigureAwait(false);

            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return false;

            return user.UserTypeId >= (ulong)minimalType;
        }

        private async Task<ulong> ExtractUserIdFromTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return 0;

            var tokenParts = token.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (tokenParts.Length != 3
                || !ulong.TryParse(tokenParts[0], out var userId)
                || !ulong.TryParse(tokenParts[1], out var userTypeId)
                || !Enum.GetValues(typeof(UserTypes)).Cast<UserTypes>().Any(_ => (ulong)_ == userTypeId))
                return 0;

            if (!_crypter.Encrypt($"{userId}_{userTypeId}").Equals(tokenParts[2]))
                return 0;

            if (!_usersCheckCache.ContainsKey(userId) || (_clock.Now - _usersCheckCache[userId]).TotalMinutes > DelayBetweenUserChecks)
            {
                var user = await _userRepository.GetUserByIdAsync(userId).ConfigureAwait(false);
                if (user == null)
                    return 0;
                else
                    _usersCheckCache.AddOrUpdate(userId, _clock.Now, (k, v) => _clock.Now);
            }

            return userId;
        }

        private static Languages GetLanguage()
        {
            return CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "fr"
                ? Languages.fr
                : Languages.en;
        }

        #endregion private generic methods
    }
}
