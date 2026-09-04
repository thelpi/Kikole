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

namespace KikoleSite.Services;

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
    private readonly IInternationalService _internationalService;
    private readonly IStringLocalizer<Translations> _resources;
    private readonly IClock _clock;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="proposalRepository">Instance of <see cref="IProposalRepository"/>.</param>
    /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
    /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
    /// <param name="playerHandler">Instance of <see cref="IPlayerHandler"/>.</param>
    /// <param name="internationalService">Instance of <see cref="IInternationalService"/>.</param>
    /// <param name="resources">Translation resources.</param>
    /// <param name="clock">Clock service.</param>
    public ProposalService(IProposalRepository proposalRepository,
        ILeaderRepository leaderRepository,
        IUserRepository userRepository,
        IPlayerHandler playerHandler,
        IInternationalService internationalService,
        IStringLocalizer<Translations> resources,
        IClock clock)
    {
        _proposalRepository = proposalRepository;
        _leaderRepository = leaderRepository;
        _userRepository = userRepository;
        _playerHandler = playerHandler;
        _internationalService = internationalService;
        _resources = resources;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ProposalResponse>> GetProposalsAsync(DateTime proposalDate, ulong userId)
    {
        var datas = await _proposalRepository
            .GetProposalsAsync(proposalDate, userId);

        var r = new List<ProposalResponse>();
        if (datas.Count > 0)
        {
            var pInfo = await _playerHandler
                .GetPlayerOfTheDayFullInfoAsync(proposalDate);

            var countryContinents = await _internationalService.GetCountryContinentsAsync();

            r = ScoreCalculator.GetProposalResponsesWithPoints(datas, pInfo, out _, _resources, countryContinents);
        }

        return r;
    }

    /// <inheritdoc />
    public async Task<(ProposalResponse, IReadOnlyCollection<ProposalDto>, LeaderDto?)> ManageProposalResponseAsync(ProposalRequest request, ulong userId, PlayerFullDto pInfo)
    {
        LeaderDto? leader = null;

        var countryContinents = await _internationalService.GetCountryContinentsAsync();

        var response = new ProposalResponse(request, pInfo, _resources, countryContinents);

        var proposalsAlready = await _proposalRepository
            .GetProposalsAsync(request.PlayerSubmissionDate, userId);

        var proposalMade = request.MatchAny(proposalsAlready);

        ScoreCalculator.GetProposalResponsesWithPoints(proposalsAlready, pInfo, out var sourcePoints, _resources, countryContinents);

        response = response.WithTotalPoints(sourcePoints, proposalMade);

        if (!proposalMade)
        {
            await _proposalRepository
                .CreateProposalAsync(request.ToDto(userId, response.Successful));

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
                    .CreateLeaderAsync(leader);
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
            .GetUserByIdAsync(userId);

        if (user == null)
            return DayGrantTypes.None;

        if (user.UserTypeId == (int)UserTypes.Administrator)
            return DayGrantTypes.Admin;

        var p = await _playerHandler
            .GetPlayerOfTheDayFullInfoAsync(date.Date);

        if (p.Player.CreationUserId == userId)
            return DayGrantTypes.Creator;

        var leaders = await _leaderRepository
            .GetUserLeadersAsync(date.Date, date.Date, true, userId);

        if (leaders.Count > 0)
            return DayGrantTypes.Found;

        var proposals = await _proposalRepository
            .GetProposalsAsync(date.Date, userId);

        if (proposals.Any(_ => _.Successful > 0 && _.ProposalTypeId == (ulong)ProposalTypes.Name))
            return DayGrantTypes.Found;

        if (proposals.Any(_ => _.ProposalTypeId == (ulong)ProposalTypes.Leaderboard))
            return DayGrantTypes.PaidBoard;

        return DayGrantTypes.None;
    }
}
