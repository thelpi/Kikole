using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Handlers;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Interfaces.Services;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;
using KikoleSite.Api.Models.Requests;

namespace KikoleSite.Api.Services
{
    /// <summary>
    /// Badge service implementation.
    /// </summary>
    /// <seealso cref="IBadgeService"/>
    public class BadgeService : IBadgeService
    {
        private const string SpecialWord = "chouse";

        private readonly IPlayerHandler _playerHandler;
        private readonly IBadgeRepository _badgeRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly IChallengeRepository _challengeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IClock _clock;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="playerHandler">Instance of <see cref="IPlayerHandler"/>.</param>
        /// <param name="badgeRepository">Instance of <see cref="IBadgeRepository"/>.</param>
        /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
        /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
        /// <param name="proposalRepository">Instance of <see cref="IProposalRepository"/>.</param>
        /// <param name="challengeRepository">Instance of <see cref="IChallengeRepository"/>.</param>
        /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
        /// <param name="clock">Clock service.</param>
        public BadgeService(IPlayerHandler playerHandler,
            IBadgeRepository badgeRepository,
            ILeaderRepository leaderRepository,
            IPlayerRepository playerRepository,
            IProposalRepository proposalRepository,
            IChallengeRepository challengeRepository,
            IUserRepository userRepository,
            IClock clock)
        {
            _playerHandler = playerHandler;
            _badgeRepository = badgeRepository;
            _leaderRepository = leaderRepository;
            _playerRepository = playerRepository;
            _proposalRepository = proposalRepository;
            _challengeRepository = challengeRepository;
            _userRepository = userRepository;
            _clock = clock;
        }

        private static readonly IReadOnlyCollection<Badges> NonRecomputableBadges
            = new List<Badges>
            {
                Badges.DoYouSpeakPatois,
                Badges.DoItYourself,
                Badges.WeAreKikole,
                Badges.AllIn,
                Badges.GambleAddiction,
                Badges.ChallengeAccepted,
                Badges.Dedicated
            };

        private static readonly IReadOnlyDictionary<Badges, Func<LeaderDto, IEnumerable<LeaderDto>, bool>> LeadersBasedBadgeCondition
            = new Dictionary<Badges, Func<LeaderDto, IEnumerable<LeaderDto>, bool>>
            {
                {
                    Badges.OverTheTopPart1,
                    (l, ls) => l.Time == ls.Min(_ => _.Time) && ls.Count(_ => _.Time == l.Time) == 1
                },
                {
                    Badges.OverTheTopPart2,
                    (l, ls) => l.Points == ls.Max(_ => _.Points) && ls.Count(_ => _.Points == l.Points) == 1
                }
            };

        private static readonly IReadOnlyDictionary<Badges, Func<LeaderDto, IEnumerable<LeaderDto>, bool>> LeadersBasedBadgeNonUniqueCondition
            = new Dictionary<Badges, Func<LeaderDto, IEnumerable<LeaderDto>, bool>>
            {
                {
                    Badges.OverTheTopPart1,
                    (l, ls) => l.Time == ls.Min(_ => _.Time)
                },
                {
                    Badges.OverTheTopPart2,
                    (l, ls) => l.Points == ls.Max(_ => _.Points)
                }
            };

        private static readonly IReadOnlyDictionary<Badges, Func<PlayerDto, bool>> PlayerBasedBadgeCondition
            = new Dictionary<Badges, Func<PlayerDto, bool>>
            {
                {
                    Badges.Archaeology,
                    p => p.YearOfBirth < 1970
                },
                {
                    Badges.WorldWarTwo,
                    p => p.YearOfBirth < 1940
                }
            };

        private static readonly IReadOnlyDictionary<Badges, Func<IEnumerable<PlayerDto>, bool>> PlayersHistoryBasedBadgeCondition
            = new Dictionary<Badges, Func<IEnumerable<PlayerDto>, bool>>
            {
                {
                    Badges.FourFourtwo,
                    ph => ph.Count(p => p.PositionId == (ulong)Positions.Goalkeeper) > 0
                        && ph.Count(p => p.PositionId == (ulong)Positions.Defender) > 3
                        && ph.Count(p => p.PositionId == (ulong)Positions.Midfielder) > 3
                        && ph.Count(p => p.PositionId == (ulong)Positions.Forward) > 1
                },
                {
                    Badges.AroundTheWorld,
                    ph => ph.Select(p => p.CountryId).Distinct().Count() >= 20
                }
            };

        // Notice "IEnumerable<ProposalDto>" in this context should not contain the final proposal
        private static readonly IReadOnlyDictionary<Badges, Func<DateTime, PlayerFullDto, IEnumerable<ProposalDto>, bool>> ProposalsBasedBadgeCondition
            = new Dictionary<Badges, Func<DateTime, PlayerFullDto, IEnumerable<ProposalDto>, bool>>
            {
                {
                    Badges.WikipediaScreenshot,
                    (d, p, ph) => ph.Any() && !ph.Any(ep => ep.ProposalTypeId != (ulong)ProposalTypes.Club)
                },
                {
                    Badges.PassportCheck,
                    (d, p, ph) => ph.Any() && !ph.Any(ep => ep.ProposalTypeId == (ulong)ProposalTypes.Club)
                },
                {
                    Badges.EverythingNotLost,
                    (d, p, ph) => ph.Any() && ph.All(ep => ep.Successful == 0)
                },
                {
                    Badges.ImFeelingLucky,
                    (d, p, ph) => !ph.Any()
                },
                {
                    Badges.OneMinuteChrono,
                    (d, p, ph) =>
                    {
                        // More than 5 clubs in the career to be eligibile
                        if (p.Clubs.Count < 5)
                            return false;

                        // Every proposal is correct
                        // Year, nationality and position are filled
                        // Easy clue is not requested
                        // Same count of club proposals than career clubs
                        if (ph.Any(_ => _.Successful == 0)
                            || !ph.Any(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Year)
                            || !ph.Any(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Position)
                            || !ph.Any(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Country)
                            || ph.Any(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Clue)
                            || ph.Count(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Club) != p.Clubs.Count)
                            return false;

                        // Less than 60 seconds
                        return (d - ph.Min(_ => _.CreationDate)).TotalSeconds < 60;
                    }
                }
            };

        private static readonly IReadOnlyDictionary<Badges, Func<LeaderDto, bool>> LeaderBasedBadgeCondition
            = new Dictionary<Badges, Func<LeaderDto, bool>>
            {
                {
                    Badges.CacaCaféClopeKikolé,
                    l => new TimeSpan(0, l.Time, 0).Hours >= 5 && new TimeSpan(0, l.Time, 0).Hours < 8
                },
                {
                    Badges.HalfwayToTheTop,
                    l => l.Points >= 500
                },
                {
                    Badges.ItsOver900,
                    l => l.Points >= 900
                },
                {
                    Badges.SavedByTheBell,
                    l => new TimeSpan(0, l.Time, 0).Hours == 23
                },
                {
                    Badges.StayUpLate,
                    l => new TimeSpan(0, l.Time, 0).Hours < 2
                },
                {
                    Badges.WoodenSpoon,
                    l => l.Points == 0
                },
                {
                    Badges.YourFirstSuccess,
                    l => true
                }
            };

        private static readonly IReadOnlyDictionary<Badges, (int, Func<LeaderDto, bool>, bool)> LeaderRunBasedBadgeCondition
            = new Dictionary<Badges, (int, Func<LeaderDto, bool>, bool)>
            {
                {
                    Badges.ThreeInARow,
                    (3, l => true, false)
                },
                {
                    Badges.AWeekInARow,
                    (7, l => true, false)
                },
                {
                    Badges.LegendTier,
                    (30, l => true, false)
                },
                {
                    Badges.MakeItDouble,
                    (2, l => l.Points == 1000, false)
                },
                {
                    Badges.TheBreakfastClub,
                    (7, l => l.Time < 540, false)
                },
                {
                    Badges.MetroBoulotKikoleDodo,
                    (7, l => l.Time >= 1260, false)
                }
            };

        private static readonly IReadOnlyDictionary<Badges, (object, int, Func<LeaderDto, object, object>, Func<object, bool>)> LeaderRunAggBasedBadgeCondition
            = new Dictionary<Badges, (object, int, Func<LeaderDto, object, object>, Func<object, bool>)>
            {
                {
                    Badges.HellOfAWeek,
                    (0, 7, (l, p) => ((int)p) + l.Points, p => (int)p >= 6666)
                }
            };

        private static readonly IReadOnlyDictionary<Badges, Func<ChallengeDto, IReadOnlyCollection<ChallengeDto>, bool>> ChallengeBasedBadgeCondition
            = new Dictionary<Badges, Func<ChallengeDto, IReadOnlyCollection<ChallengeDto>, bool>>
            {
                {
                    Badges.GambleAddiction,
                    (c, ch) => ch.Count(ac => ac.HostUserId == c.HostUserId) >= 5
                },
                {
                    Badges.AllIn,
                    (c, ch) => c.PointsRate >= 80
                },
                {
                    Badges.ChallengeAccepted,
                    (c, ch) => true
                }
            };

        /// <inheritdoc />
        public async Task ManageChallengesBasedBadgesAsync(ulong challengeId)
        {
            var challenge = await _challengeRepository
                .GetChallengeByIdAsync(challengeId)
                .ConfigureAwait(false);

            var allAccepted = await _challengeRepository
               .GetAcceptedChallengesAsync(null, null)
               .ConfigureAwait(false);

            foreach (var user in new[] { challenge.HostUserId, challenge.GuestUserId })
            {
                if (ChallengeBasedBadgeCondition[Badges.ChallengeAccepted](challenge, allAccepted))
                {
                    await AddBadgeToUserAsync(
                            Badges.ChallengeAccepted, user)
                        .ConfigureAwait(false);
                }


                if (ChallengeBasedBadgeCondition[Badges.AllIn](challenge, allAccepted))
                {
                    await AddBadgeToUserAsync(
                            Badges.AllIn, user)
                        .ConfigureAwait(false);
                }
            }

            if (ChallengeBasedBadgeCondition[Badges.GambleAddiction](challenge, allAccepted))
            {
                await AddBadgeToUserAsync(
                        Badges.GambleAddiction, challenge.HostUserId)
                    .ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task ResetBadgesAsync(Languages language)
        {
            var allBadges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            foreach (var badge in allBadges.Where(b => !NonRecomputableBadges.Contains((Badges)b.Id)))
            {
                await _badgeRepository
                    .ResetBadgeDatasAsync(badge.Id)
                    .ConfigureAwait(false);
            }

            var firstDate = await _playerRepository
                .GetFirstDateAsync()
                .ConfigureAwait(false);

            var endDate = _clock.Today;

            var playersHistoryFull = await _playerRepository
                .GetPlayersOfTheDayAsync(firstDate.Date, endDate)
                .ConfigureAwait(false);

            var leadersHistoryFull = await GetLeadersHistoryAsync(
                    endDate, firstDate.Date)
                .ConfigureAwait(false);

            var date = firstDate.Date;
            while (date <= endDate)
            {
                var leaders = leadersHistoryFull
                    .Where(lhf => lhf.ProposalDate == date);

                var leadersHistory = leadersHistoryFull
                    .Where(lhf => lhf.ProposalDate <= date)
                    .ToList();

                foreach (var leader in leaders)
                {
                    var proposals = await _proposalRepository
                        .GetProposalsAsync(date, leader.UserId)
                        .ConfigureAwait(false);

                    // remove the final name proposal
                    proposals = proposals
                        .Where(p => p.Successful == 0 || p.ProposalTypeId != (ulong)ProposalTypes.Name)
                        .ToList();

                    var pDay = playersHistoryFull.Single(phl => phl.ProposalDate == date);

                    var playersHistory = playersHistoryFull
                        .Where(phl => phl.ProposalDate <= date)
                        .ToList();

                    await PrepareNewLeaderBadgesInternalAsync(
                            leader, pDay, proposals, allBadges, leadersHistory, playersHistory, language)
                        .ConfigureAwait(false);
                }

                date = date.AddDays(1);
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<UserBadge>> PrepareNewLeaderBadgesAsync(
            LeaderDto leader,
            PlayerDto playerOfTheDay,
            IReadOnlyCollection<ProposalDto> proposalsBeforeWin,
            Languages language)
        {
            var allBadges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            var firstDate = await _playerRepository
                .GetFirstDateAsync()
                .ConfigureAwait(false);

            var leadersHistory = await GetLeadersHistoryAsync(
                    leader.ProposalDate, firstDate.Date)
                .ConfigureAwait(false);

            var playersHistory = await _playerRepository
                .GetPlayersOfTheDayAsync(firstDate.Date, leader.ProposalDate)
                .ConfigureAwait(false);

            return await PrepareNewLeaderBadgesInternalAsync(
                    leader, playerOfTheDay, proposalsBeforeWin, allBadges, leadersHistory, playersHistory, language)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<UserBadge>> PrepareNonLeaderBadgesAsync(
            ulong userId, BaseProposalRequest request, Languages language)
        {
            var collectedBadges = new List<ulong>();

            var allBadges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            // We have only a special badge base on the request value for now
            if (SpecialWord.Equals(request.Value.ToLowerInvariant()))
            {
                await InsertBadgeIfNotAlreadyAsync(
                        request.ProposalDateTime, userId, (ulong)Badges.DoYouSpeakPatois, collectedBadges, allBadges)
                    .ConfigureAwait(false);
            }

            if (request.IsTodayPlayer)
            {
                var proposals = await _proposalRepository
                    .GetAllProposalsDateExactAsync(userId)
                    .ConfigureAwait(false);

                var playersCreated = await _playerRepository
                    .GetPlayersByCreatorAsync(userId, true)
                    .ConfigureAwait(false);

                var i = 1;
                var date = _clock.Today;
                while (i < 30)
                {
                    date = date.AddDays(-1);
                    if (!proposals.Any(p => p.ProposalDate == date)
                        && !playersCreated.Any(p => p.ProposalDate == date))
                        break;
                    i++;
                }

                if (i == 30)
                {
                    await InsertBadgeIfNotAlreadyAsync(
                            request.ProposalDateTime, userId, (ulong)Badges.Dedicated, collectedBadges, allBadges)
                        .ConfigureAwait(false);
                }
            }

            return await GetUserBadgesAsync(
                    collectedBadges, request.ProposalDateTime, allBadges, language)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> AddBadgeToUserAsync(Badges badge, ulong userId)
        {
            var allBadges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            var collectedBadges = new List<ulong>();

            await InsertBadgeIfNotAlreadyAsync(
                    _clock.Now, userId, (ulong)badge, collectedBadges, allBadges)
                .ConfigureAwait(false);

            return collectedBadges.Count > 0;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Badge>> GetAllBadgesAsync(Languages language)
        {
            var dtos = await _badgeRepository
                .GetBadgesAsync(false)
                .ConfigureAwait(false);

            var badges = new List<Badge>(dtos.Count);
            foreach (var dto in dtos)
            {
                var b = await GetBadgeAsync(
                        dto.Id, dtos, language)
                    .ConfigureAwait(false);
                badges.Add(b);
            }

            return badges
                .OrderByDescending(b => b.Users)
                .ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(
            ulong userId,
            ulong connectedUserId,
            Languages language)
        {
            var isAllowedToSeeHiddenBadge = connectedUserId == userId;
            if (connectedUserId > 0 && !isAllowedToSeeHiddenBadge)
            {
                var userDto = await _userRepository
                    .GetUserByIdAsync(connectedUserId)
                    .ConfigureAwait(false);

                isAllowedToSeeHiddenBadge = userDto?.UserTypeId == (ulong)UserTypes.Administrator;
            }

            var badges = await _badgeRepository
               .GetBadgesAsync(true)
               .ConfigureAwait(false);

            var dtos = await _badgeRepository
                .GetUserBadgesAsync(userId)
                .ConfigureAwait(false);

            var badgesFull = new List<UserBadge>();
            foreach (var dto in dtos)
            {
                var b = badges.Single(_ => _.Id == dto.BadgeId);

                if (_clock.Today == dto.GetDate
                    && b.Hidden > 0
                    && !isAllowedToSeeHiddenBadge)
                {
                    continue;
                }

                var ub = await GetUserBadgeAsync(
                        dto.BadgeId, badges, dto.GetDate, language)
                    .ConfigureAwait(false);

                badgesFull.Add(ub);
            }

            return badgesFull
                .OrderByDescending(b => b.Hidden)
                .ThenBy(b => b.Users)
                .ToList();
        }

        private async Task<IReadOnlyCollection<UserBadge>> PrepareNewLeaderBadgesInternalAsync(
            LeaderDto leader,
            PlayerDto playerOfTheDay,
            IReadOnlyCollection<ProposalDto> proposalsBeforeWin,
            IReadOnlyCollection<BadgeDto> allBadges,
            IReadOnlyCollection<LeaderDto> leadersHistory,
            IReadOnlyCollection<PlayerDto> playersHistory,
            Languages language)
        {
            var collectedBadges = new List<ulong>();

            var myPlayerHistory = playersHistory
                .Where(p => leadersHistory.Any(h => h.UserId == leader.UserId && h.ProposalDate == p.ProposalDate));

            var myCreatedPlayers = playersHistory
                .Where(p => p.CreationUserId == leader.UserId);

            var playerFull = await _playerHandler
                .GetPlayerFullInfoAsync(playerOfTheDay)
                .ConfigureAwait(false);

            // Badges you can got only if you find the player today
            if (leader.IsCurrentDay)
            {
                var leaders = leadersHistory
                    .Where(lh => lh.ProposalDate == leader.ProposalDate && lh.IsCurrentDay);

                var myDay1History = leadersHistory
                    .Where(lh => lh.UserId == leader.UserId && lh.IsCurrentDay);

                foreach (var badge in LeaderBasedBadgeCondition.Keys)
                {
                    if (LeaderBasedBadgeCondition[badge](leader))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }

                foreach (var badge in LeaderRunBasedBadgeCondition.Keys)
                {
                    var (runCount, checkFunc, incPlayerSubmission) = LeaderRunBasedBadgeCondition[badge];

                    var respectConditions = RespectLeadersRunConditions(leader,
                        myDay1History, myCreatedPlayers,
                        runCount, checkFunc, incPlayerSubmission);

                    if (respectConditions)
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }

                foreach (var badge in LeaderRunAggBasedBadgeCondition.Keys)
                {
                    var (initialValue, runCount, aggFunc, checkFunc) = LeaderRunAggBasedBadgeCondition[badge];

                    var respectConditions = RespectLeadersRunConditions(leader,
                        myDay1History, myCreatedPlayers,
                        initialValue, runCount, aggFunc, checkFunc);

                    if (respectConditions)
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }

                foreach (var badge in LeadersBasedBadgeCondition.Keys)
                {
                    var hasBadgeNotAlone = LeadersBasedBadgeNonUniqueCondition[badge](leader, leaders);
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

                        if (LeadersBasedBadgeCondition[badge](leader, leaders))
                        {
                            await InsertBadgeIfNotAlreadyAsync(
                                     leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges)
                                 .ConfigureAwait(false);
                        }
                    }
                }

                foreach (var badge in ProposalsBasedBadgeCondition.Keys)
                {
                    if (ProposalsBasedBadgeCondition[badge](leader.CreationDate, playerFull, proposalsBeforeWin))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }
            }

            foreach (var badge in PlayerBasedBadgeCondition.Keys)
            {
                if (PlayerBasedBadgeCondition[badge](playerOfTheDay))
                {
                    await InsertBadgeIfNotAlreadyAsync(
                             leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges)
                         .ConfigureAwait(false);
                }
            }

            foreach (var badge in PlayersHistoryBasedBadgeCondition.Keys)
            {
                if (PlayersHistoryBasedBadgeCondition[badge](myPlayerHistory))
                {
                    await InsertBadgeIfNotAlreadyAsync(
                            leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges)
                        .ConfigureAwait(false);
                }
            }

            if (playerOfTheDay.BadgeId.HasValue)
            {
                await InsertBadgeIfNotAlreadyAsync(
                        leader.ProposalDate, leader.UserId, playerOfTheDay.BadgeId.Value, collectedBadges, allBadges)
                    .ConfigureAwait(false);
            }

            return await GetUserBadgesAsync(
                    collectedBadges, leader.ProposalDate, allBadges, language)
                .ConfigureAwait(false);
        }

        private static bool RespectLeadersRunConditions(LeaderDto leader,
            IEnumerable<LeaderDto> myHistory,
            IEnumerable<PlayerDto> myCreatedPlayers,
            int runLength,
            Func<LeaderDto, bool> funcConditionOnLeader,
            bool creatorIncludeInRun)
        {
            return RespectLeadersRunConditionsInternal(leader, myHistory, myCreatedPlayers, runLength,
                funcConditionOnLeader, creatorIncludeInRun, null, null, null);
        }

        private static bool RespectLeadersRunConditions(LeaderDto leader,
            IEnumerable<LeaderDto> myHistory,
            IEnumerable<PlayerDto> myCreatedPlayers,
            object initialValue,
            int runLength,
            Func<LeaderDto, object, object> aggFunc,
            Func<object, bool> checkFunc)
        {
            return RespectLeadersRunConditionsInternal(leader, myHistory, myCreatedPlayers, runLength,
                null, false, initialValue, aggFunc, checkFunc);
        }

        private static bool RespectLeadersRunConditionsInternal(LeaderDto leader,
            IEnumerable<LeaderDto> myHistory,
            IEnumerable<PlayerDto> myCreatedPlayers,
            int runLength,
            Func<LeaderDto, bool> funcConditionOnLeader,
            bool creatorIncludeInRun,
            object initialValue,
            Func<LeaderDto, object, object> aggFunc,
            Func<object, bool> checkFunc)
        {
            object agg = initialValue;
            var i = 0;
            var dateToConsider = leader.ProposalDate;
            do
            {
                var isCreator = myCreatedPlayers.Any(mcp => mcp.ProposalDate == dateToConsider);

                if (!isCreator)
                {
                    var dateMeLeader = myHistory.FirstOrDefault(mh => mh.ProposalDate == dateToConsider);
                    if (dateMeLeader == null || (funcConditionOnLeader != null && !funcConditionOnLeader(dateMeLeader)))
                        break;
                    if (aggFunc != null)
                        agg = aggFunc(dateMeLeader, agg);
                    i++;
                }
                else if (creatorIncludeInRun)
                    i++;
                dateToConsider = dateToConsider.AddDays(-1);
            }
            while (i < runLength);
            return i == runLength && (checkFunc == null || checkFunc(agg));
        }

        private async Task InsertBadgeIfNotAlreadyAsync(
            DateTime proposalDate,
            ulong userId,
            ulong badge,
            List<ulong> collectedBadges,
            IReadOnlyCollection<BadgeDto> allBadges)
        {
            var hasBadge = await _badgeRepository
                .CheckUserHasBadgeAsync(userId, badge)
                .ConfigureAwait(false);

            if (!hasBadge)
            {
                var badgeMatch = allBadges.Single(b => b.Id == badge);

                // badge can apply only after the creation date of the badge
                if (badgeMatch.CreationDate.Date <= proposalDate.Date)
                {
                    await _badgeRepository
                        .InsertUserBadgeAsync(new UserBadgeDto
                        {
                            GetDate = proposalDate.Date,
                            BadgeId = badge,
                            UserId = userId
                        })
                        .ConfigureAwait(false);

                    collectedBadges.Add(badge);
                }
            }
        }

        private async Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(
            List<ulong> collectedBadges,
            DateTime proposalDate,
            IReadOnlyCollection<BadgeDto> allBadges,
            Languages language)
        {
            var collectedUserBadges = new List<UserBadge>();

            foreach (var badge in collectedBadges)
            {
                var ub = await GetUserBadgeAsync(
                        badge, allBadges, proposalDate, language)
                    .ConfigureAwait(false);

                collectedUserBadges.Add(ub);
            }

            return collectedUserBadges;
        }

        private async Task<UserBadge> GetUserBadgeAsync(
            ulong badge,
            IReadOnlyCollection<BadgeDto> badgesDto,
            DateTime proposalDate,
            Languages language)
        {
            var b = await GetBadgeAsync(
                    badge, badgesDto, language)
                .ConfigureAwait(false);
            return new UserBadge(b, proposalDate);
        }

        private async Task<Badge> GetBadgeAsync(
            ulong badge,
            IReadOnlyCollection<BadgeDto> badgesDto,
            Languages language)
        {
            string description = null;

            if (language != Languages.en)
            {
                description = await _badgeRepository
                    .GetBadgeDescriptionAsync(badge, (ulong)language)
                    .ConfigureAwait(false);
            }

            var users = await _badgeRepository
                .GetUsersWithBadgeAsync(badge)
                .ConfigureAwait(false);

            return new Badge(badgesDto.Single(_ => _.Id == badge), users.Count, description);
        }

        private async Task<IReadOnlyCollection<LeaderDto>> GetLeadersHistoryAsync(
            DateTime date,
            DateTime firstDate)
        {
            var leadersHistory = new List<LeaderDto>();

            while (date.Date >= firstDate.Date)
            {
                var leadersBefore = await _leaderRepository
                    .GetLeadersAtDateAsync(date, false)
                    .ConfigureAwait(false);
                leadersHistory.AddRange(leadersBefore);
                date = date.AddDays(-1);
            }

            return leadersHistory;
        }
    }
}
