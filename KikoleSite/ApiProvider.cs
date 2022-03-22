using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using KikoleSite.Api;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KikoleSite
{
    public class ApiProvider : IApiProvider
    {
        private readonly HttpClient _client;

        private static readonly Dictionary<ulong, IReadOnlyDictionary<ulong, string>> _countriesCache
             = new Dictionary<ulong, IReadOnlyDictionary<ulong, string>>();
        private static ProposalChart _proposalChartCache;
        private static IReadOnlyCollection<Club> _clubsCache;

        public ApiProvider(IConfiguration configuration)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(configuration.GetValue<string>("BackApiBaseUrl"))
            };
        }

        public async Task<string> CreateAccountAsync(string login,
            string password, string question, string answer)
        {
            var response = await SendAsync(
                    "users",
                    HttpMethod.Post,
                    null,
                    new
                    {
                        login,
                        password,
                        passwordResetQuestion = question,
                        passwordResetAnswer = answer?.Trim()
                    })
                .ConfigureAwait(false);
            
            return response.IsSuccessStatusCode
                ? null
                : $"Creation failed: {response.StatusCode}";
        }

        public async Task<(bool, string)> LoginAsync(string login, string password)
        {
            var response = await SendAsync(
                    $"users/{login}/authentication-tokens?password={password}",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return (false, $"Authentication failed: {response.StatusCode}");

            var token = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token))
                return (false, $"Authentication failed: invalid token");

            return (true, token);
        }

        public async Task<ProposalResponse> SubmitProposalAsync(DateTime proposalDate,
            string value,
            int daysBefore,
            ProposalType proposalType,
            string authToken)
        {
            var response = await SendAsync(
                    $"{proposalType.ToString().ToLowerInvariant()}-proposals",
                    HttpMethod.Put,
                    authToken,
                    new
                    {
                        proposalDate,
                        value,
                        daysBefore
                    })
                .ConfigureAwait(false);
            
            return await GetResponseContentAsync<ProposalResponse>(response)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyDictionary<ulong, string>> GetCountriesAsync(ulong languageId)
        {
            if (!_countriesCache.ContainsKey(languageId))
            {
                var response = await SendAsync(
                    $"countries?languageId={languageId}",
                    HttpMethod.Get)
                .ConfigureAwait(false);

                var apiCountries = await GetResponseContentAsync<IReadOnlyCollection<Country>>(response)
                    .ConfigureAwait(false);

                _countriesCache.Add(
                    languageId,
                    apiCountries
                        .ToDictionary(ac => ac.Code, ac => ac.Name));
            }

            return _countriesCache[languageId];
        }

        public async Task<ProposalChart> GetProposalChartAsync()
        {
            if (_proposalChartCache == null)
            {
                var response = await SendAsync(
                       "proposal-charts",
                       HttpMethod.Get)
                   .ConfigureAwait(false);

                _proposalChartCache = await GetResponseContentAsync<ProposalChart>(response)
                    .ConfigureAwait(false);
            }

            return _proposalChartCache;
        }

        public async Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(
            DateTime proposalDate, string authToken)
        {
            var response = await SendAsync(
                    $"proposals?proposalDate={proposalDate.Date.ToString("yyyy-MM-dd")}",
                    HttpMethod.Get,
                    authToken)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<IReadOnlyCollection<ProposalResponse>>(response)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<Leader>> GetLeadersAsync(
            LeaderSort leaderSort, DateTime? minimalDate)
        {
            var response = await SendAsync(
                    $"leaders?minimalDate={minimalDate?.ToString("yyyy-MM-dd")}&leaderSort={(int)leaderSort}",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<IReadOnlyCollection<Leader>>(response)
                .ConfigureAwait(false);
        }

        public async Task<string> GetClueAsync(DateTime proposalDate)
        {
            var response = await SendAsync(
                    $"player-clues?proposalDate={proposalDate.ToString("yyyy-MM-dd")}",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<string>(response)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<Club>> GetClubsAsync(bool resetCache = false)
        {
            if (_clubsCache == null || resetCache)
            {
                var response = await SendAsync(
                    "clubs",
                    HttpMethod.Get)
                .ConfigureAwait(false);

                _clubsCache = await GetResponseContentAsync<IReadOnlyCollection<Club>>(response)
                    .ConfigureAwait(false);
            }

            return _clubsCache;
        }

        public async Task<IReadOnlyCollection<Leader>> GetTodayLeadersAsync()
        {
            var response = await SendAsync(
                    "today-leaders",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<IReadOnlyCollection<Leader>>(response)
                .ConfigureAwait(false);
        }

        public async Task<UserStats> GetUserStatsAsync(ulong id)
        {
            var response = await SendAsync($"users/{id}/stats", HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<UserStats>(response, () => null)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(ulong userId)
        {
            var response = await SendAsync($"users/{userId}/badges", HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<IReadOnlyCollection<UserBadge>>(response)
                .ConfigureAwait(false);
        }

        public async Task<string> CreateClubAsync(string name,
            IReadOnlyList<string> allowedNames, string authToken)
        {
            var response = await SendAsync(
                    "clubs",
                    HttpMethod.Post,
                    authToken,
                    new
                    {
                        name,
                        allowedNames
                    })
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? null
                : $"StatusCode: {response.StatusCode}";
        }

        public async Task<string> CreatePlayerAsync(PlayerRequest player, string authToken)
        {
            var response = await SendAsync(
                    "players",
                    HttpMethod.Post,
                    authToken,
                    player)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? null
                : $"StatusCode: {response.StatusCode}";
        }

        public async Task<IReadOnlyCollection<Badge>> GetBadgesAsync()
        {
            var response = await SendAsync(
                    "badges",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<IReadOnlyCollection<Badge>>(response)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<string>> GetUserKnownPlayersAsync(string authToken)
        {
            var response = await SendAsync(
                    "users/known-players", HttpMethod.Get, authToken)
                .ConfigureAwait(false);
            
            return await GetResponseContentAsync<IReadOnlyCollection<string>>(response)
                .ConfigureAwait(false);
        }

        public async Task<string> GetCurrentMessageAsync()
        {
            var response = await SendAsync(
                    "current-messages", HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<string>(response)
                .ConfigureAwait(false);
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
            var response = await SendAsync(
                    $"user-passwords?oldp={currentPassword}&newp={newPassword}",
                    HttpMethod.Put,
                    authToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? null
                : $"Invalid response: {response.StatusCode}";
        }

        public async Task<string> ChangeQAndAAsync(string authToken,
            string question, string answer)
        {
            var response = await SendAsync(
                    $"user-questions?question={question}&answer={answer}",
                    HttpMethod.Patch,
                    authToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? null
                : $"Invalid response: {response.StatusCode}";
        }

        public async Task<string> ResetPasswordAsync(string login,
            string answer, string newPassword)
        {
            var response = await SendAsync(
                    $"reset-passwords?login={login}&answer={answer}&newPassword={newPassword}",
                    HttpMethod.Patch)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? null
                : $"Invalid response: {response.StatusCode}";
        }

        public async Task<(bool, string)> GetLoginQuestionAsync(string login)
        {
            var response = await SendAsync(
                    $"users/{login}/questions",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return (false, $"Invalid response: {response.StatusCode}");

            return (true, await GetResponseContentAsync<string>(response)
                .ConfigureAwait(false));
        }

        public async Task<PlayerCreator> IsPlayerOfTheDayUser(
            DateTime proposalDate, string authToken)
        {
            var response = await SendAsync(
                    $"player-of-the-day-users?proposalDate={proposalDate.ToString("yyyy-MM-dd")}",
                    HttpMethod.Get,
                    authToken)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<PlayerCreator>(
                    response, () => null)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<Player>> GetPlayerSubmissionsAsync(string authToken)
        {
            var response = await SendAsync(
                    "player-submissions",
                    HttpMethod.Get,
                    authToken)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<IReadOnlyCollection<Player>>(response)
                .ConfigureAwait(false);
        }

        public async Task<string> ValidatePlayerSubmissionAsync(
            PlayerSubmissionValidationRequest request, string authToken)
        {
            var response = await SendAsync(
                    "player-submissions",
                    HttpMethod.Post,
                    authToken,
                    request)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return null;

            return $"Invalid response: {response.StatusCode}";
        }

        private async Task<HttpResponseMessage> SendAsync(string route, HttpMethod method,
            string authToken = null, object content = null)
        {
            var request = new HttpRequestMessage
            {
                Content = content == null
                    ? null
                    : new StringContent(
                            JsonConvert.SerializeObject(content),
                            Encoding.UTF8,
                            "application/json"),
                Method = method,
                RequestUri = new Uri(route, UriKind.Relative)
            };

            if (!string.IsNullOrWhiteSpace(authToken))
                request.Headers.Add("AuthToken", authToken);

            return await _client
                .SendAsync(request)
                .ConfigureAwait(false);
        }

        private async Task<T> GetResponseContentAsync<T>(HttpResponseMessage response,
            Func<T> fallbackIsNotSucess = null)
        {
            if (fallbackIsNotSucess == null)
                response.EnsureSuccessStatusCode();
            else if (!response.IsSuccessStatusCode)
                return fallbackIsNotSucess();

            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return typeof(T) == typeof(string)
                ? (T)Convert.ChangeType(content, typeof(T))
                : JsonConvert.DeserializeObject<T>(content);
        }

        private async Task<bool> IsTypeOfUserAsync(string authToken, UserTypes minimalType)
        {
            var response = await SendAsync(
                    "user-types", HttpMethod.Get, authToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return false;

            var value = await GetResponseContentAsync<string>(response)
                .ConfigureAwait(false);

            return value != null
                && ulong.TryParse(value, out var id)
                && Enum.GetValues(typeof(UserTypes)).Cast<UserTypes>().Any(_ => (ulong)_ == id)
                && id >= (ulong)minimalType;
        }
    }
}
