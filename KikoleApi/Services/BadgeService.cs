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

namespace KikoleApi.Services
{
    public class BadgeService : IBadgeService
    {
        private const string SpecialWord = "chouse";
        private const ulong TheEndPlayerId = 81;

        private static readonly Badges[] NonRecomputableBadges = new[]
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

        private readonly IBadgeRepository _badgeRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly TextResources _resources;
        private readonly IClock _clock;

        public BadgeService(IBadgeRepository badgeRepository,
            ILeaderRepository leaderRepository,
            IPlayerRepository playerRepository,
            IProposalRepository proposalRepository,
            TextResources resources,
            IClock clock)
        {
            _badgeRepository = badgeRepository;
            _leaderRepository = leaderRepository;
            _playerRepository = playerRepository;
            _proposalRepository = proposalRepository;
            _resources = resources;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task ResetBadgesAsync()
        {
            var allBadges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            foreach (var badge in allBadges)
                await ResetBadgeAsync((Badges)badge.Id).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task ResetBadgeAsync(Badges badge)
        {
            if (NonRecomputableBadges.Contains(badge))
                return;

            var firstDate = await _playerRepository
                .GetFirstDateAsync()
                .ConfigureAwait(false);

            await _badgeRepository
                .ResetBadgeDatasAsync((ulong)badge)
                .ConfigureAwait(false);

            var date = firstDate.Date;
            var endDate = _clock.Now.Date;
            while (date <= endDate)
            {
                var leaders = await _leaderRepository
                    .GetLeadersAtDateAsync(date)
                    .ConfigureAwait(false);

                var pDay = await _playerRepository
                    .GetPlayerOfTheDayAsync(date)
                    .ConfigureAwait(false);

                foreach (var leader in leaders)
                {
                    var proposals = await _proposalRepository
                        .GetProposalsAsync(date, leader.UserId)
                        .ConfigureAwait(false);

                    // remove the final name proposal
                    proposals = proposals
                        .Where(p => p.Successful == 0 || p.ProposalTypeId != (ulong)ProposalTypes.Name)
                        .ToList();

                    await PrepareNewLeaderBadgesAsync(
                            leader, pDay, proposals, true)
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
            var collectedBadges = new List<Badges>();

            var allBadges = await _badgeRepository
                .GetBadgesAsync(true)
                .ConfigureAwait(false);

            // Badges you can got only if you find the player today
            if (isActualTodayleader)
            {
                var firstDate = await _playerRepository
                    .GetFirstDateAsync()
                    .ConfigureAwait(false);
                
                var leadersHistory = await _leaderRepository
                    .GetLeadersHistoryAsync(leader.ProposalDate, firstDate.Date)
                    .ConfigureAwait(false);
                
                var playersHistory = await _playerRepository
                    .GetPlayersOfTheDayAsync(firstDate.Date, leader.ProposalDate)
                    .ConfigureAwait(false);

                var leaders = leadersHistory
                    .SelectMany(lh => lh)
                    .Where(lh => lh.ProposalDate == leader.ProposalDate);

                var myHistory = leadersHistory
                    .SelectMany(lh => lh)
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

                foreach (var badge in PlayersHistoryBadgeCondition.Keys)
                {
                    if (PlayersHistoryBadgeCondition[badge](myPlayerHistory))
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
            if (playerOfTheDay.Id == TheEndPlayerId)
            {
                var oldCount = collectedBadges.Count;
                await InsertBadgeIfNotAlreadyAsync(
                        leader.ProposalDate, leader.UserId, Badges.TheEnd, collectedBadges, allBadges)
                    .ConfigureAwait(false);

                // Inserts the consolation prize ONLY if no "TheEnd" badge got
                if (collectedBadges.Count == oldCount)
                {
                    await InsertBadgeIfNotAlreadyAsync(
                            leader.ProposalDate, leader.UserId, Badges.TheEndConsolationPrize, collectedBadges, allBadges)
                        .ConfigureAwait(false);
                }
            }

            return await GetUserBadgesAsync(
                    collectedBadges, leader.ProposalDate, allBadges)
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

                if (_clock.Now.Date == dto.GetDate
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

                // the badge need to be created BEFORE
                // avoid getting badges during recomputation
                var allowed = badgeMatch.CreationDate.Date <= proposalDate.Date;
                if (allowed && badgeMatch.IsUnique != 0)
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
            if (_resources.Language != Languages.en)
            {
                description = await _badgeRepository
                    .GetBadgeDescriptionAsync((ulong)badge, (ulong)_resources.Language)
                    .ConfigureAwait(false);
            }

            var users = await _badgeRepository
                .GetUsersWithBadgeAsync((ulong)badge)
                .ConfigureAwait(false);

            return new Badge(badgesDto.Single(_ => _.Id == (ulong)badge), users.Count, description);
        }

        internal static IReadOnlyDictionary<Badges, Func<LeaderDto, IEnumerable<LeaderDto>, bool>> LeadersBasedBadgeCondition
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

        internal static IReadOnlyDictionary<Badges, Func<LeaderDto, IEnumerable<LeaderDto>, bool>> LeadersBasedBadgeNonUniqueCondition
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

        internal static IReadOnlyDictionary<Badges, Func<PlayerDto, bool>> PlayerBasedBadgeCondition
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

        internal static IReadOnlyDictionary<Badges, Func<IEnumerable<PlayerDto>, bool>> PlayersHistoryBadgeCondition
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

        // Notice "IEnumerable<ProposalDto>" does not contain the final proposal
        internal static IReadOnlyDictionary<Badges, Func<IEnumerable<ProposalDto>, bool>> ProposalsBasedBadgeCondition
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
            };

        internal static IReadOnlyDictionary<Badges, Func<LeaderDto, bool>> LeaderBasedBadgeCondition
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

        internal static IReadOnlyDictionary<Badges, (int, Func<LeaderDto, bool>, bool)> LeaderRunBasedBadgeCondition
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
    }
}
