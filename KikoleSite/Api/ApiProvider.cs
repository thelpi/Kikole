using System;
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
            string value, int daysBefore, ProposalType proposalType, string authToken)
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
