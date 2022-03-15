using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KikoleSite.Api
{
    public interface IApiProvider
    {
        Task<(bool success, string value)> CreateAccountAsync(string login,
            string password, string question, string answer);

        Task<ProposalResponse> SubmitProposalAsync(DateTime proposalDate,
            string value,
            int daysBefore,
            ProposalType proposalType,
            string authToken,
            int sourcePoints);

        Task<(bool success, string value)> LoginAsync(string login, string password);

        Task<IReadOnlyCollection<CountryKvp>> GetCountriesAsync(ulong languageId);

        Task<ProposalChart> GetProposalChartAsync();

        Task<(bool success, IReadOnlyCollection<ProposalResponse> proposals)> GetProposalsAsync(
            DateTime proposalDate, string authToken);

        Task<IReadOnlyCollection<Leader>> GetLeadersAsync(LeaderSort leaderSort,
            int limit, DateTime? minimalDate);

        Task<string> GetClueAsync(DateTime proposalDate);

        Task<IReadOnlyCollection<Club>> GetClubsAsync();
    }
}
