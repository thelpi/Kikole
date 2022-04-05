﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;

namespace KikoleApi.Services
{
    public class BadgeService : IBadgeService
    {
        private const string SpecialWord = "chouse";
        private const ulong TheEndPlayerId = 81;

        private readonly IBadgeRepository _badgeRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly TextResources _resources;
        private readonly IClock _clock;

        public BadgeService(IBadgeRepository badgeRepository,
            ILeaderRepository leaderRepository,
            IPlayerRepository playerRepository,
            TextResources resources,
            IClock clock)
        {
            _badgeRepository = badgeRepository;
            _leaderRepository = leaderRepository;
            _playerRepository = playerRepository;
            _resources = resources;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<UserBadge>> PrepareNewLeaderBadgesAsync(
            LeaderDto leader, PlayerDto playerOfTheDay,
            IReadOnlyCollection<ProposalDto> proposalsBeforeWin,
            bool isActualTodayleader)
        {
            var collectedBadges = new List<Badges>();

            var allBadges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            if (isActualTodayleader)
            {
                var firstDate = await _playerRepository
                    .GetFirstDateAsync()
                    .ConfigureAwait(false);

                var leadersHistory = await _leaderRepository
                    .GetLeadersHistoryAsync(leader.ProposalDate, firstDate.Date)
                    .ConfigureAwait(false);

                foreach (var badge in BadgeHelper.LeaderBasedBadgeCondition.Keys)
                {
                    if (BadgeHelper.LeaderBasedBadgeCondition[badge](leader, leadersHistory))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }

                foreach (var badge in BadgeHelper.PlayerBasedBadgeCondition.Keys)
                {
                    if (BadgeHelper.PlayerBasedBadgeCondition[badge](playerOfTheDay))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                 leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
                             .ConfigureAwait(false);
                    }
                }

                var leaders = await _leaderRepository
                    .GetLeadersAtDateAsync(leader.ProposalDate)
                    .ConfigureAwait(false);

                foreach (var badge in BadgeHelper.LeadersBasedBadgeCondition.Keys)
                {
                    var hasBadgeNotAlone = BadgeHelper.LeadersBasedBadgeNonUniqueCondition[badge](leader, leaders);
                    if (hasBadgeNotAlone)
                    {
                        var badgeOwners = await _badgeRepository
                            .GetUsersOfTheDayWithBadgeAsync((ulong)badge, leader.ProposalDate)
                            .ConfigureAwait(false);

                        foreach (var bo in badgeOwners)
                        {
                            await _badgeRepository
                                .RemoveUserBadgeAsync(new UserBadgeDto
                                {
                                    BadgeId = (ulong)badge,
                                    UserId = bo.UserId
                                })
                                .ConfigureAwait(false);
                        }

                        if (BadgeHelper.LeadersBasedBadgeCondition[badge](leader, leaders))
                        {
                            await InsertBadgeIfNotAlreadyAsync(
                                     leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
                                 .ConfigureAwait(false);
                        }
                    }
                }

                var myHistory = leadersHistory
                    .SelectMany(lh => lh)
                    .Where(lh => lh.UserId == leader.UserId)
                    .ToList();

                var playersHistory = new List<PlayerDto>();
                foreach (var mh in myHistory)
                {
                    var p = await _playerRepository
                        .GetPlayerOfTheDayAsync(mh.ProposalDate.Date)
                        .ConfigureAwait(false);
                    playersHistory.Add(p);
                }
                playersHistory.Add(playerOfTheDay);

                foreach (var badge in BadgeHelper.PlayersHistoryBadgeCondition.Keys)
                {
                    if (BadgeHelper.PlayersHistoryBadgeCondition[badge](playersHistory))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }

                if (!proposalsBeforeWin.Any(ep => ep.ProposalTypeId != (ulong)ProposalTypes.Club))
                {
                    await InsertBadgeIfNotAlreadyAsync(
                            leader.ProposalDate, leader.UserId, Badges.WikipediaScreenshot, collectedBadges, allBadges)
                        .ConfigureAwait(false);
                }

                if (!proposalsBeforeWin.Any(ep => ep.ProposalTypeId == (ulong)ProposalTypes.Club))
                {
                    await InsertBadgeIfNotAlreadyAsync(
                            leader.ProposalDate, leader.UserId, Badges.PassportCheck, collectedBadges, allBadges)
                        .ConfigureAwait(false);
                }

                if (proposalsBeforeWin.Count > 0 && proposalsBeforeWin.All(ep => ep.Successful == 0))
                {
                    await InsertBadgeIfNotAlreadyAsync(
                            leader.ProposalDate, leader.UserId, Badges.EverythingNotLost, collectedBadges, allBadges)
                        .ConfigureAwait(false);
                }
            }

            if (playerOfTheDay.Id == TheEndPlayerId)
            {
                await InsertBadgeIfNotAlreadyAsync(
                        leader.ProposalDate, leader.UserId, Badges.B34, collectedBadges, allBadges)
                    .ConfigureAwait(false);
            }

            return await GetUserBadgesAsync(
                    collectedBadges, leader.UserId, leader.ProposalDate, allBadges)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<UserBadge>> PrepareNonLeaderBadgesAsync(
            ulong userId, BaseProposalRequest request)
        {
            var collectedBadges = new List<Badges>();

            var allBadges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            if (SpecialWord.Equals(request.Value.ToLowerInvariant()))
            {
                await InsertBadgeIfNotAlreadyAsync(
                        request.ProposalDate, userId, Badges.DoYouSpeakPatois, collectedBadges, allBadges)
                    .ConfigureAwait(false);
            }

            return await GetUserBadgesAsync(
                    collectedBadges, userId, request.ProposalDate, allBadges)
                .ConfigureAwait(false);
        }

        private async Task InsertBadgeIfNotAlreadyAsync(DateTime proposalDate,
            ulong userId, Badges badge, List<Badges> collectedBadges,
            IReadOnlyCollection<BadgeDto> allBadges)
        {
            var hasBadge = await _badgeRepository
                .CheckUserHasBadgeAsync(userId, (ulong)badge)
                .ConfigureAwait(false);

            if (!hasBadge)
            {
                var badgeMatch = allBadges.Single(b => b.Id == (ulong)badge);

                var allowed = true;
                if (badgeMatch.IsUnique != 0)
                {
                    var users = await _badgeRepository
                        .GetUsersWithBadgeAsync(badgeMatch.Id)
                        .ConfigureAwait(false);
                    if (users.Count > 0)
                        allowed = false;
                }

                if (allowed)
                {
                    await _badgeRepository
                        .InsertUserBadgeAsync(new UserBadgeDto
                        {
                            GetDate = proposalDate.Date,
                            BadgeId = (ulong)badge,
                            UserId = userId
                        })
                        .ConfigureAwait(false);

                    collectedBadges.Add(badge);
                }
            }
        }

        private async Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(
            List<Badges> collectedBadges, ulong userId, DateTime proposalDate,
            IReadOnlyCollection<BadgeDto> allBadges)
        {
            var collectedUserBadges = new List<UserBadge>();

            foreach (var badge in collectedBadges)
            {
                var ub = await GetUserBadgeAsync(
                        badge, allBadges, proposalDate, userId)
                    .ConfigureAwait(false);

                collectedUserBadges.Add(ub);
            }

            return collectedUserBadges;
        }

        private async Task<UserBadge> GetUserBadgeAsync(Badges badge,
            IReadOnlyCollection<BadgeDto> badgesDto,
            DateTime proposalDate,
            ulong userId)
        {
            string description = null;
            if (_resources.Language != Languages.en)
            {
                description = await _badgeRepository
                    .GetBadgeDescriptionAsync((ulong)badge, (ulong)_resources.Language)
                    .ConfigureAwait(false);
            }

            var users = await _badgeRepository
                .GetUsersWithBadgeAsync((ulong)badge)
                .ConfigureAwait(false);

            return new UserBadge(
                badgesDto.Single(_ => _.Id == (ulong)badge),
                new UserBadgeDto
                {
                    BadgeId = (ulong)badge,
                    GetDate = proposalDate,
                    UserId = userId
                },
                users.Count,
                description);
        }
    }
}
