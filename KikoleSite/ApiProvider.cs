using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KikoleSite
{
    public class ApiProvider
    {
        private readonly HttpClient _client;

        public ApiProvider(IConfiguration configuration)
        {
            var backApiBaseUrl = configuration.GetValue<string>("BackApiBaseUrl");
            _client = new HttpClient
            {
                BaseAddress = new Uri(backApiBaseUrl)
            };
        }

        public async Task<(string, bool)> LoginAsync(string login, string password)
        {
            var response = await _client
                .GetAsync(new Uri($"users/{login}/authentication-tokens?password={password}", UriKind.Relative))
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return ($"Authentication failed: {response.StatusCode}", false);

            var token = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
                return ($"Authentication failed: invalid token", false);

            return (token, true);
        }

        public async Task<ProposalResponse> SubmitProposalAsync(ProposalRequest request,
            ProposalType proposalType)
        {
            var route = "";
            switch (proposalType)
            {
                case ProposalType.Club:
                    route = "club-proposals";
                    break;
                case ProposalType.Clue:
                    route = "clue-proposals";
                    break;
                case ProposalType.Country:
                    route = "country-proposals";
                    break;
                case ProposalType.Name:
                    route = "name-proposals";
                    break;
                case ProposalType.Year:
                    route = "year-proposals";
                    break;
            }

            var response = await _client
                .PutAsJsonAsync(new Uri(route, UriKind.Relative), request)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<ProposalResponse>(content);
        }
    }
}
