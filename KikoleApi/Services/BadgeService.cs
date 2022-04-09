using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;
using Microsoft.AspNetCore.Http;

namespace KikoleApi.Services
{
    public class BadgeService : IBadgeService
    {
        private const string SpecialWord = "chouse";

        private readonly IBadgeRepository _badgeRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly IChallengeRepository _challengeRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IClock _clock;

        private static readonly IReadOnlyCollection<Badges> NonRecomputableBadges
            = new List<Badges>
            {
                Badges.DoYouSpeakPatois,
                Badges.TheEndConsolationPrize,
                Badges.TheEnd,
                Badges.DoItYourself,
                Badges.WeAreKikole,
                Badges.AllIn,
                Badges.GambleAddiction,
                Badges.ChallengeAccepted
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
                },
                {
                    Badges.ThirdWaveFeminism,
                    p => p.BadgeId.HasValue && p.BadgeId == (ulong)Badges.ThirdWaveFeminism
                },
                {
                    Badges.ItsAFuckingDisgrace,
                    p => p.BadgeId.HasValue && p.BadgeId == (ulong)Badges.ItsAFuckingDisgrace
                },
                {
                    Badges.CaptainTsubasa,
                    p => p.BadgeId.HasValue && p.BadgeId == (ulong)Badges.CaptainTsubasa
                },
                {
                    Badges.KikolesCreatorFriend,
                    p => p.BadgeId.HasValue && p.BadgeId == (ulong)Badges.KikolesCreatorFriend
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
        private static readonly IReadOnlyDictionary<Badges, Func<IEnumerable<ProposalDto>, bool>> ProposalsBasedBadgeCondition
            = new Dictionary<Badges, Func<IEnumerable<ProposalDto>, bool>>
            {
                {
                    Badges.WikipediaScreenshot,
                    ph => !ph.Any(ep => ep.ProposalTypeId != (ulong)ProposalTypes.Club)
                },
                {
                    Badges.PassportCheck,
                    ph => !ph.Any(ep => ep.ProposalTypeId == (ulong)ProposalTypes.Club)
                },
                {
                    Badges.EverythingNotLost,
                    ph => ph.Any() && ph.All(ep => ep.Successful == 0)
                },
                {
                    Badges.ImFeelingLucky,
                    ph => !ph.Any()
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

        public BadgeService(IBadgeRepository badgeRepository,
            ILeaderRepository leaderRepository,
            IPlayerRepository playerRepository,
            IProposalRepository proposalRepository,
            IChallengeRepository challengeRepository,
            IHttpContextAccessor httpContextAccessor,
            IClock clock)
        {
            _badgeRepository = badgeRepository;
            _leaderRepository = leaderRepository;
            _playerRepository = playerRepository;
            _proposalRepository = proposalRepository;
            _challengeRepository = challengeRepository;
            _httpContextAccessor = httpContextAccessor;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task ManageChallengesBasedBadgesAsync(ChallengeDto challenge)
        {
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
        public async Task ResetBadgesAsync()
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
                            leader, pDay, proposals, true,
                            allBadges, date, leadersHistory, playersHistory)
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
            bool isActualTodayleader)
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
                    leader, playerOfTheDay, proposalsBeforeWin, isActualTodayleader,
                    allBadges, firstDate, leadersHistory, playersHistory)
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

            // We have only a special badge base on the request value for now
            if (SpecialWord.Equals(request.Value.ToLowerInvariant()))
            {
                await InsertBadgeIfNotAlreadyAsync(
                        request.ProposalDate, userId, Badges.DoYouSpeakPatois, collectedBadges, allBadges)
                    .ConfigureAwait(false);
            }

            return await GetUserBadgesAsync(
                    collectedBadges, request.ProposalDate, allBadges)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> AddBadgeToUserAsync(Badges badge, ulong userId)
        {
            var allBadges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            var collectedBadges = new List<Badges>();

            await InsertBadgeIfNotAlreadyAsync(
                    _clock.Now, userId, badge, collectedBadges, allBadges)
                .ConfigureAwait(false);

            return collectedBadges.Count > 0;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Badge>> GetAllBadgesAsync()
        {
            var dtos = await _badgeRepository
                .GetBadgesAsync(false)
                .ConfigureAwait(false);

            var badges = new List<Badge>(dtos.Count);
            foreach (var dto in dtos)
            {
                var b = await GetBadgeAsync(
                        (Badges)dto.Id, dtos)
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
            bool isAllowedToSeeHiddenBadge)
        {
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
                        (Badges)dto.BadgeId, badges, dto.GetDate)
                    .ConfigureAwait(false);

                badgesFull.Add(ub);
            }

            return badgesFull
                .OrderByDescending(b => b.Unique)
                .ThenByDescending(b => b.Hidden)
                .ThenBy(b => b.Users)
                .ToList();
        }

        private async Task<IReadOnlyCollection<UserBadge>> PrepareNewLeaderBadgesInternalAsync(
            LeaderDto leader,
            PlayerDto playerOfTheDay,
            IReadOnlyCollection<ProposalDto> proposalsBeforeWin,
            bool isActualTodayleader,
            IReadOnlyCollection<BadgeDto> allBadges,
            DateTime firstDate,
            IReadOnlyCollection<LeaderDto> leadersHistory,
            IReadOnlyCollection<PlayerDto> playersHistory)
        {
            var collectedBadges = new List<Badges>();

            // Badges you can got only if you find the player today
            if (isActualTodayleader)
            {
                var leaders = leadersHistory
                    .Where(lh => lh.ProposalDate == leader.ProposalDate);

                var myHistory = leadersHistory
                    .Where(lh => lh.UserId == leader.UserId);

                var myPlayerHistory = playersHistory
                    .Where(p => myHistory.Any(h => h.ProposalDate == p.ProposalDate));

                var myCreatedPlayers = playersHistory
                    .Where(p => p.CreationUserId == leader.UserId);

                foreach (var badge in LeaderBasedBadgeCondition.Keys)
                {
                    if (LeaderBasedBadgeCondition[badge](leader))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }

                foreach (var badge in LeaderRunBasedBadgeCondition.Keys)
                {
                    var conditions = LeaderRunBasedBadgeCondition[badge];

                    bool respectConditions = RespectLeadersRunConditions(leader,
                        myHistory, myCreatedPlayers,
                        conditions.Item1, conditions.Item2, conditions.Item3);

                    if (respectConditions)
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }

                foreach (var badge in PlayerBasedBadgeCondition.Keys)
                {
                    if (PlayerBasedBadgeCondition[badge](playerOfTheDay))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                 leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
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
                                     leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
                                 .ConfigureAwait(false);
                        }
                    }
                }

                foreach (var badge in PlayersHistoryBasedBadgeCondition.Keys)
                {
                    if (PlayersHistoryBasedBadgeCondition[badge](myPlayerHistory))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }

                foreach (var badge in ProposalsBasedBadgeCondition.Keys)
                {
                    if (ProposalsBasedBadgeCondition[badge](proposalsBeforeWin))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                leader.ProposalDate, leader.UserId, badge, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }
            }

            // Not generic (special badge)
            if (playerOfTheDay.BadgeId == (ulong)Badges.TheEnd)
            {
                await InsertBadgeIfNotAlreadyAsync(
                        leader.ProposalDate, leader.UserId, Badges.TheEnd, collectedBadges, allBadges)
                    .ConfigureAwait(false);
            }

            return await GetUserBadgesAsync(
                    collectedBadges, leader.ProposalDate, allBadges)
                .ConfigureAwait(false);
        }

        private static bool RespectLeadersRunConditions(LeaderDto leader,
            IEnumerable<LeaderDto> myHistory,
            IEnumerable<PlayerDto> myCreatedPlayers,
            int runLength,
            Func<LeaderDto, bool> funcConditionOnLeader,
            bool creatorIncludeInRun)
        {
            var i = 0;
            var dateToConsider = leader.ProposalDate;
            do
            {
                var isCreator = myCreatedPlayers.Any(mcp => mcp.ProposalDate == dateToConsider);

                if (!isCreator)
                {
                    var dateMeLeader = myHistory.FirstOrDefault(mh => mh.ProposalDate == dateToConsider);
                    if (dateMeLeader == null || !funcConditionOnLeader(dateMeLeader))
                        break;
                    i++;
                }
                else if (creatorIncludeInRun)
                    i++;
                dateToConsider = dateToConsider.AddDays(-1);
            }
            while (i < runLength);
            return i == runLength;
        }

        private async Task InsertBadgeIfNotAlreadyAsync(
            DateTime proposalDate,
            ulong userId,
            Badges badge,
            List<Badges> collectedBadges,
            IReadOnlyCollection<BadgeDto> allBadges)
        {
            var hasBadge = await _badgeRepository
                .CheckUserHasBadgeAsync(userId, (ulong)badge)
                .ConfigureAwait(false);

            if (!hasBadge)
            {
                var badgeMatch = allBadges.Single(b => b.Id == (ulong)badge);

                // badge can apply only after the creation date of the badge
                var allowed = badgeMatch.CreationDate.Date <= proposalDate.Date;

                if (allowed && badgeMatch.IsUnique != 0)
                {
                    // badge is unique: check if another user already has the badge
                    var users = await _badgeRepository
                        .GetUsersWithBadgeAsync(badgeMatch.Id)
                        .ConfigureAwait(false);

                    allowed = users.Count == 0;

                    // tries to add the substitution badge if exists
                    if (!allowed && badgeMatch.SubBadgeId.HasValue)
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                proposalDate, userId, (Badges)badgeMatch.SubBadgeId.Value, collectedBadges, allBadges)
                            .ConfigureAwait(false);
                    }
                }

                if (allowed)
                {
                    // the badge we try to attach to the user is a substitution badge
                    var sourceBadgeofSub = allBadges.SingleOrDefault(b => b.SubBadgeId == (ulong)badge);
                    if (sourceBadgeofSub != null)
                    {
                        var hasSourceBadge = await _badgeRepository
                            .CheckUserHasBadgeAsync(userId, sourceBadgeofSub.Id)
                            .ConfigureAwait(false);

                        // for a better badge the user already has
                        allowed = !hasSourceBadge && sourceBadgeofSub.CreationDate.Date <= proposalDate.Date;
                    }
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
            List<Badges> collectedBadges,
            DateTime proposalDate,
            IReadOnlyCollection<BadgeDto> allBadges)
        {
            var collectedUserBadges = new List<UserBadge>();

            foreach (var badge in collectedBadges)
            {
                var ub = await GetUserBadgeAsync(
                        badge, allBadges, proposalDate)
                    .ConfigureAwait(false);

                collectedUserBadges.Add(ub);
            }

            return collectedUserBadges;
        }

        private async Task<UserBadge> GetUserBadgeAsync(
            Badges badge,
            IReadOnlyCollection<BadgeDto> badgesDto,
            DateTime proposalDate)
        {
            var b = await GetBadgeAsync(
                    badge, badgesDto)
                .ConfigureAwait(false);
            return new UserBadge(b, proposalDate);
        }

        private async Task<Badge> GetBadgeAsync(
            Badges badge,
            IReadOnlyCollection<BadgeDto> badgesDto)
        {
            string description = null;

            var lng = _httpContextAccessor.ExtractLanguage();
            if (lng != Languages.en)
            {
                description = await _badgeRepository
                    .GetBadgeDescriptionAsync((ulong)badge, (ulong)lng)
                    .ConfigureAwait(false);
            }

            var users = await _badgeRepository
                .GetUsersWithBadgeAsync((ulong)badge)
                .ConfigureAwait(false);

            return new Badge(badgesDto.Single(_ => _.Id == (ulong)badge), users.Count, description);
        }

        private async Task<IReadOnlyCollection<LeaderDto>> GetLeadersHistoryAsync(
            DateTime date,
            DateTime firstDate)
        {
            var leadersHistory = new List<LeaderDto>();

            while (date.Date >= firstDate.Date)
            {
                var leadersBefore = await _leaderRepository
                    .GetLeadersAtDateAsync(date)
                    .ConfigureAwait(false);
                leadersHistory.AddRange(leadersBefore);
                date = date.AddDays(-1);
            }

            return leadersHistory;
        }
    }
}
