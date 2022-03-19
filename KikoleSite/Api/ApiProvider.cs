using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KikoleSite.Api
{
    public class ApiProvider : IApiProvider
    {
        private readonly HttpClient _client;

        public ApiProvider(IConfiguration configuration)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(configuration.GetValue<string>("BackApiBaseUrl"))
            };
        }

        public async Task<(bool, string)> CreateAccountAsync(string login,
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

            if (!response.IsSuccessStatusCode)
                return (false, $"Creation failed: {response.StatusCode}");

            return (true, null);
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
            string authToken,
            int sourcePoints)
        {
            var response = await SendAsync(
                    $"{proposalType.ToString().ToLowerInvariant()}-proposals",
                    HttpMethod.Put,
                    authToken,
                    new
                    {
                        proposalDate,
                        value,
                        daysBefore,
                        sourcePoints
                    })
                .ConfigureAwait(false);
            
            return await GetResponseContentAsync<ProposalResponse>(response)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<CountryKvp>> GetCountriesAsync(ulong languageId)
        {
            var response = await SendAsync(
                    $"countries?languageId={languageId}",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<IReadOnlyCollection<CountryKvp>>(response)
                .ConfigureAwait(false);
        }

        public async Task<ProposalChart> GetProposalChartAsync()
        {
            var response = await SendAsync(
                    "proposal-charts",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<ProposalChart>(response)
                .ConfigureAwait(false);
        }

        public async Task<(bool, IReadOnlyCollection<ProposalResponse>)> GetProposalsAsync(
            DateTime proposalDate, string authToken)
        {
            var response = await SendAsync(
                    $"proposals?proposalDate={proposalDate.Date.ToString("yyyy-MM-dd")}",
                    HttpMethod.Get,
                    authToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return (false, null);

            var proposals = await GetResponseContentAsync<IReadOnlyCollection<ProposalResponse>>(response)
                .ConfigureAwait(false);

            return (true, proposals);
        }

        public async Task<IReadOnlyCollection<Leader>> GetLeadersAsync(LeaderSort leaderSort,
            int limit, DateTime? minimalDate)
        {
            var response = await SendAsync(
                    $"leaders?minimalDate={minimalDate?.ToString("yyyy-MM-dd")}&leaderSort={(int)leaderSort}",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            var datas = await GetResponseContentAsync<IReadOnlyCollection<Leader>>(response)
                .ConfigureAwait(false);

            uint? totalPoints = null;
            TimeSpan? bestTime = null;
            int? successCount = null;
            int currentPos = 0;
            return datas
                .Select((d, i) =>
                {
                    switch (leaderSort)
                    {
                        case LeaderSort.BestTime:
                            if (!bestTime.HasValue || bestTime != d.BestTime)
                            {
                                currentPos = i + 1;
                                bestTime = d.BestTime;
                            }
                            break;
                        case LeaderSort.SuccessCount:
                            if (!successCount.HasValue || successCount != d.SuccessCount)
                            {
                                currentPos = i + 1;
                                successCount = d.SuccessCount;
                            }
                            break;
                        case LeaderSort.TotalPoints:
                            if (!totalPoints.HasValue || totalPoints != d.TotalPoints)
                            {
                                currentPos = i + 1;
                                totalPoints = d.TotalPoints;
                            }
                            break;
                    }
                    d.Position = currentPos;

                    return d;
                })
                .Take(limit)
                .ToList();
        }

        public async Task<string> GetClueAsync(DateTime proposalDate)
        {
            var response = await SendAsync(
                    $"player-clues?proposalDate={proposalDate.ToString("yyyy-MM-dd")}",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return "";

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<Club>> GetClubsAsync()
        {
            var response = await SendAsync(
                    "clubs",
                    HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<IReadOnlyCollection<Club>>(response)
                .ConfigureAwait(false);
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

        public async Task<UserStats> GetUserStats(ulong id)
        {
            var response = await SendAsync($"users/{id}/stats", HttpMethod.Get)
                .ConfigureAwait(false);

            return await GetResponseContentAsync<UserStats>(response)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<UserBadge>> GetUserBadges(ulong userId)
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

        public async Task<string> CreatePlayerAsync(
            PlayerRequest player, string authToken)
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

        private async Task<HttpResponseMessage> SendAsync(string route, HttpMethod method,
            string authToken = null,
            object content = null)
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

        private async Task<T> GetResponseContentAsync<T>(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}
