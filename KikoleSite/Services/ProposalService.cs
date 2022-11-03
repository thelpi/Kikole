using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Handlers;
using KikoleSite.Helpers;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Services
{
    /// <summary>
    /// Proposal service implementation.
    /// </summary>
    /// <seealso cref="IProposalService"/>
    public class ProposalService : IProposalService
    {
        private readonly IProposalRepository _proposalRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPlayerHandler _playerHandler;
        private readonly IStringLocalizer<Translations> _resources;
        private readonly IClock _clock;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="proposalRepository">Instance of <see cref="IProposalRepository"/>.</param>
        /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
        /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
        /// <param name="playerHandler">Instance of <see cref="IPlayerHandler"/>.</param>
        /// <param name="resources">Translation resources.</param>
        /// <param name="clock">Clock service.</param>
        public ProposalService(IProposalRepository proposalRepository,
            ILeaderRepository leaderRepository,
            IUserRepository userRepository,
            IPlayerHandler playerHandler,
            IStringLocalizer<Translations> resources,
            IClock clock)
        {
            _proposalRepository = proposalRepository;
            _leaderRepository = leaderRepository;
            _userRepository = userRepository;
            _playerHandler = playerHandler;
            _resources = resources;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(DateTime proposalDate, ulong userId)
        {
            var datas = await _proposalRepository
                .GetProposalsAsync(proposalDate, userId)
                .ConfigureAwait(false);

            var r = new List<ProposalResponse>();
            if (datas.Count > 0)
            {
                var pInfo = await _playerHandler
                    .GetPlayerOfTheDayFullInfoAsync(proposalDate)
                    .ConfigureAwait(false);

                r = GetProposalResponsesWithPoints(datas, pInfo, out _, _resources);
            }

            return r;
        }

        /// <inheritdoc />
        public async Task<(ProposalResponse, IReadOnlyCollection<ProposalDto>, LeaderDto)> ManageProposalResponseAsync<T>(T request, ulong userId, PlayerFullDto pInfo)
            where T : BaseProposalRequest
        {
            LeaderDto leader = null;

            var response = new ProposalResponse(request, pInfo, _resources);

            var proposalsAlready = await _proposalRepository
                .GetProposalsAsync(request.PlayerSubmissionDate, userId)
                .ConfigureAwait(false);

            var proposalMade = request.MatchAny(proposalsAlready);

            GetProposalResponsesWithPoints(proposalsAlready, pInfo, out var sourcePoints, _resources);

            response = response.WithTotalPoints(sourcePoints, proposalMade);

            if (!proposalMade)
            {
                await _proposalRepository
                    .CreateProposalAsync(request.ToDto(userId, response.Successful))
                    .ConfigureAwait(false);

                if (response.IsWin)
                {
                    leader = new LeaderDto
                    {
                        Points = (ushort)response.TotalPoints,
                        ProposalDate = request.PlayerSubmissionDate,
                        Time = (_clock.Now - request.PlayerSubmissionDate).ToRoundMinutes(),
                        UserId = userId,
                        CreationDate = _clock.Now
                    };

                    await _leaderRepository
                        .CreateLeaderAsync(leader)
                        .ConfigureAwait(false);
                }
            }

            return (response, proposalsAlready, leader);
        }

        /// <inheritdoc />
        public async Task<DayGrantTypes> GetGrantAccessForDayAsync(ulong userId, DateTime date)
        {
            if (userId == 0)
                return DayGrantTypes.None;

            var user = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (user == null)
                return DayGrantTypes.None;

            if (user.UserTypeId == (int)UserTypes.Administrator)
                return DayGrantTypes.Admin;

            var p = await _playerHandler
                .GetPlayerOfTheDayFullInfoAsync(date.Date)
                .ConfigureAwait(false);

            if (p.Player.CreationUserId == userId)
                return DayGrantTypes.Creator;

            var leaders = await _leaderRepository
                .GetUserLeadersAsync(date.Date, date.Date, true, userId)
                .ConfigureAwait(false);

            if (leaders.Count > 0)
                return DayGrantTypes.Found;

            var proposals = await _proposalRepository
                .GetProposalsAsync(date.Date, userId)
                .ConfigureAwait(false);

            if (proposals.Any(_ => _.ProposalTypeId == (ulong)ProposalTypes.Leaderboard))
                return DayGrantTypes.PaidBoard;

            return DayGrantTypes.None;
        }

        internal static List<ProposalResponse> GetProposalResponsesWithPoints(
            IEnumerable<ProposalDto> proposalDtos,
            PlayerFullDto player,
            out int points,
            IStringLocalizer<Translations> resources)
        {
            var totalPoints = ProposalChart.BasePoints;
            var proposals = proposalDtos
                .OrderBy(pDto => pDto.CreationDate)
                .Select(pDto =>
                {
                    var pr = new ProposalResponse(pDto, player, resources)
                        .WithTotalPoints(totalPoints, false);
                    totalPoints = pr.TotalPoints;
                    return pr;
                })
                .ToList();

            points = totalPoints;
            return proposals;
        }
    }
}
