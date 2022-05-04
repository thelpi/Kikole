using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Helpers;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Handlers;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Interfaces.Services;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Requests;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Api.Services
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

            var r = new List<ProposalResponse>(0);
            if (datas.Count > 0)
            {
                var pInfo = await _playerHandler
                    .GetPlayerOfTheDayFullInfoAsync(proposalDate)
                    .ConfigureAwait(false);

                r = GetProposalResponsesWithPoints(datas, pInfo, out _);
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

            GetProposalResponsesWithPoints(proposalsAlready, pInfo, out int sourcePoints);

            response = response.WithTotalPoints(sourcePoints, proposalMade);

            if (!proposalMade)
            {
                await _proposalRepository
                    .CreateProposalAsync(request.ToDto(userId, response.Successful))
                    .ConfigureAwait(false);

                if (response.IsWin)
                {
                    await _leaderRepository
                        .CreateLeaderAsync(new LeaderDto
                        {
                            Points = (ushort)response.TotalPoints,
                            ProposalDate = request.PlayerSubmissionDate,
                            Time = (_clock.Now - request.PlayerSubmissionDate).ToRoundMinutes(),
                            UserId = userId,
                            CreationDate = _clock.Now
                        })
                        .ConfigureAwait(false);
                }
            }

            return (response, proposalsAlready, leader);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<User>> GetUsersWithProposalAsync(DateTime proposalDate)
        {
            var proposals = await _proposalRepository
                .GetProposalsAsync(proposalDate)
                .ConfigureAwait(false);

            var userIds = proposals
                .Select(p => p.UserId)
                .Distinct()
                .ToList();

            var users = new List<User>(userIds.Count);

            foreach (var userId in userIds)
            {
                var user = await _userRepository
                    .GetUserByIdAsync(userId)
                    .ConfigureAwait(false);

                users.Add(new User(user));
            }

            return users;
        }

        private List<ProposalResponse> GetProposalResponsesWithPoints(
            IEnumerable<ProposalDto> proposalDtos,
            PlayerFullDto player,
            out int points)
        {
            var totalPoints = ProposalChart.Default.BasePoints;
            var proposals = proposalDtos
                .OrderBy(pDto => pDto.CreationDate)
                .Select(pDto =>
                {
                    var pr = new ProposalResponse(pDto, player, _resources)
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
