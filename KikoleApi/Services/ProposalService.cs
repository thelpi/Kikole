using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Interfaces.Repositories;
using KikoleApi.Interfaces.Services;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Requests;
using Microsoft.Extensions.Localization;

namespace KikoleApi.Services
{
    public class ProposalService : IProposalService
    {
        private readonly IProposalRepository _proposalRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IBadgeService _badgeService;
        private readonly IPlayerService _playerService;
        private readonly IStringLocalizer<Translations> _resources;
        private readonly IClock _clock;

        public ProposalService(IProposalRepository proposalRepository,
            ILeaderRepository leaderRepository,
            IBadgeService badgeService,
            IPlayerService playerService,
            IStringLocalizer<Translations> resources,
            IClock clock)
        {
            _proposalRepository = proposalRepository;
            _leaderRepository = leaderRepository;
            _badgeService = badgeService;
            _playerService = playerService;
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
                var pInfo = await _playerService
                    .GetPlayerInfoAsync(proposalDate)
                    .ConfigureAwait(false);

                r = GetProposalResponsesWithPoints(datas, pInfo, out _);
            }

            return r;
        }

        /// <inheritdoc />
        public async Task<ProposalResponse> ManageProposalResponseAsync<T>(T request, ulong userId, PlayerFullDto pInfo)
            where T : BaseProposalRequest
        {
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
                    var leader = new LeaderDto
                    {
                        Points = (ushort)response.TotalPoints,
                        ProposalDate = request.ProposalDate.Date,
                        Time = Convert.ToUInt16(Math.Ceiling((_clock.Now - request.ProposalDate.Date).TotalMinutes)),
                        UserId = userId
                    };

                    var isToday = request.IsTodayPlayer;
                    if (isToday)
                    {
                        await _leaderRepository
                            .CreateLeaderAsync(leader)
                            .ConfigureAwait(false);
                    }

                    var leaderBadges = await _badgeService
                        .PrepareNewLeaderBadgesAsync(leader, pInfo.Player, proposalsAlready, isToday)
                        .ConfigureAwait(false);

                    foreach (var b in leaderBadges)
                        response.AddBadge(b);
                }
            }

            var proposalBadges = await _badgeService
                .PrepareNonLeaderBadgesAsync(userId, request)
                .ConfigureAwait(false);

            foreach (var b in proposalBadges)
                response.AddBadge(b);

            return response;
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
                    var pr = new ProposalResponse(pDto, player)
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
