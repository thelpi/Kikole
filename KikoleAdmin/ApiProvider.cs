﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using KikoleAdmin.Properties;
using Newtonsoft.Json;

namespace KikoleAdmin
{
    public class ApiProvider
    {
        private readonly HttpClient _client;
        const ulong DefaultLanguageId = 1;

        public ApiProvider()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(Settings.Default.BackApiBaseUrl)
            };
            _client.DefaultRequestHeaders.Add("AuthToken",
                "4_1_2e0913992cab0860dac21a6b2c24baab046983071341f63bf5c4244cf8b13cb5");
        }

        public async Task AddPlayerAsync(PlayerRequest player)
        {
            var response = await _client
                .PostAsync(
                    new Uri("players", UriKind.Relative),
                    new StringContent(
                        JsonConvert.SerializeObject(player),
                        Encoding.UTF8,
                        "application/json"))
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task AddClubAsync(Club club)
        {
            var response = await _client
                .PostAsync(
                    new Uri("clubs", UriKind.Relative),
                    new StringContent(
                        JsonConvert.SerializeObject(club),
                        Encoding.UTF8,
                        "application/json"))
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyCollection<Country>> GetCountriesAsync()
        {
            var response = await _client
                .GetAsync(
                    new Uri($"countries?languageId={DefaultLanguageId}", UriKind.Relative))
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IReadOnlyCollection<Country>>(content);
        }

        public async Task<IReadOnlyCollection<Club>> GetClubsAsync()
        {
            var response = await _client
                .GetAsync(
                    new Uri($"clubs", UriKind.Relative))
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IReadOnlyCollection<Club>>(content);
        }

        public async Task RecomputeLeaderboardAsync()
        {
            var response = await _client
                .PutAsync("leaders-computing", null)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }
}
