using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Handlers;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;

namespace KikoleSite.Services;

/// <summary>
/// Badge service implementation.
/// </summary>
/// <seealso cref="IBadgeService"/>
public class BadgeService : IBadgeService
{
    private readonly IPlayerHandler _playerHandler;
    private readonly IBadgeRepository _badgeRepository;
    private readonly ILeaderRepository _leaderRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IProposalRepository _proposalRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClock _clock;
    private readonly IGameCalendar _gameCalendar;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="playerHandler">Instance of <see cref="IPlayerHandler"/>.</param>
    /// <param name="badgeRepository">Instance of <see cref="IBadgeRepository"/>.</param>
    /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
    /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
    /// <param name="proposalRepository">Instance of <see cref="IProposalRepository"/>.</param>
    /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
    /// <param name="clock">Clock service.</param>
    /// <param name="gameCalendar">Instance of <see cref="IGameCalendar"/>.</param>
    public BadgeService(IPlayerHandler playerHandler,
        IBadgeRepository badgeRepository,
        ILeaderRepository leaderRepository,
        IPlayerRepository playerRepository,
        IProposalRepository proposalRepository,
        IUserRepository userRepository,
        IClock clock,
        IGameCalendar gameCalendar)
    {
        _playerHandler = playerHandler;
        _badgeRepository = badgeRepository;
        _leaderRepository = leaderRepository;
        _playerRepository = playerRepository;
        _proposalRepository = proposalRepository;
        _userRepository = userRepository;
        _clock = clock;
        _gameCalendar = gameCalendar;
    }

    private static readonly IReadOnlyCollection<Badges> NonRecomputableBadges
        = new List<Badges>
        {
            Badges.DoItYourself,
            Badges.WeAreKikole,
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
                    // au moins 5 clubs dans la carriere pour etre eligible
                    if (p.Clubs.Count < 5)
                        return false;

                    // Every proposal is correct
                    // Year, nationality and position are filled (le continent est deduit
                    // du pays desormais, pas besoin d'une proposition Continent separee)
                    // Easy clue is not requested
                    // Leaderboard is not requested
                    // Same count of club proposals than career clubs
                    if (ph.Any(_ => _.Successful == 0)
                        || !ph.Any(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Year)
                        || !ph.Any(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Position)
                        || !ph.Any(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Country)
                        || ph.Any(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Clue)
                        || ph.Any(_ => (ProposalTypes)_.ProposalTypeId == ProposalTypes.Leaderboard)
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

    // l'accumulateur est un cumul de points : le typer en int evite le boxing et les
    // castes aveugles de l'ancienne version basee sur object
    private static readonly IReadOnlyDictionary<Badges, (int, int, Func<LeaderDto, int, int>, Func<int, bool>)> LeaderRunAggBasedBadgeCondition
        = new Dictionary<Badges, (int, int, Func<LeaderDto, int, int>, Func<int, bool>)>
        {
            {
                Badges.HellOfAWeek,
                (0, 7, (l, p) => p + l.Points, p => p >= 6666)
            }
        };

    /// <inheritdoc />
    public async Task ResetBadgesAsync(Languages language)
    {
        var allBadges = await _badgeRepository
            .GetBadgesAsync(true);

        foreach (var badge in allBadges.Where(b => !NonRecomputableBadges.Contains((Badges)b.Id)))
        {
            await _badgeRepository
                .ResetBadgeDatasAsync(badge.Id);
        }

        var endDate = _clock.Today;

        var playersHistoryFull = await _playerRepository
            .GetPlayersOfTheDayAsync(_gameCalendar.HiddenDate, endDate);

        var leadersHistoryFull = await GetLeadersHistoryAsync(
                endDate, _gameCalendar.HiddenDate);

        var date = _gameCalendar.HiddenDate;
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
                    .GetProposalsAsync(date, leader.UserId);

                // remove the final name proposal
                proposals = proposals
                    .Where(p => p.Successful == 0 || p.ProposalTypeId != (ulong)ProposalTypes.Name)
                    .ToList();

                var pDay = playersHistoryFull.Single(phl => phl.PublicationDate == date);

                var playersHistory = playersHistoryFull
                    .Where(phl => phl.PublicationDate <= date)
                    .ToList();

                await PrepareNewLeaderBadgesInternalAsync(
                        leader, pDay, proposals, allBadges, leadersHistory, playersHistory, language);
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
            .GetBadgesAsync(true);

        var leadersHistory = await GetLeadersHistoryAsync(
                leader.ProposalDate, _gameCalendar.FirstDate);

        var playersHistory = await _playerRepository
            .GetPlayersOfTheDayAsync(_gameCalendar.FirstDate, leader.ProposalDate);

        return await PrepareNewLeaderBadgesInternalAsync(
                leader, playerOfTheDay, proposalsBeforeWin, allBadges, leadersHistory, playersHistory, language);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<UserBadge>> PrepareNonLeaderBadgesAsync(
        ulong userId, ProposalRequest request, Languages language)
    {
        var collectedBadges = new List<ulong>();

        var allBadges = await _badgeRepository
            .GetBadgesAsync(true);

        if (request.IsTodayPlayer)
        {
            var proposals = await _proposalRepository
                .GetAllProposalsDateExactAsync(userId);

            var playersCreated = await _playerRepository
                .GetPlayersByCreatorAsync(userId, true);

            var i = 1;
            var date = _clock.Today;
            while (i < 30)
            {
                date = date.AddDays(-1);
                if (!proposals.Any(p => p.ProposalDate == date)
                    && !playersCreated.Any(p => p.PublicationDate == date))
                    break;
                i++;
            }

            if (i == 30)
            {
                await InsertBadgeIfNotAlreadyAsync(
                        request.ProposalDateTime, userId, (ulong)Badges.Dedicated, collectedBadges, allBadges);
            }
        }

        return await GetUserBadgesAsync(
                collectedBadges, request.ProposalDateTime, allBadges, language);
    }

    /// <inheritdoc />
    public async Task<bool> AddBadgeToUserAsync(Badges badge, ulong userId)
    {
        var allBadges = await _badgeRepository
            .GetBadgesAsync(true);

        var collectedBadges = new List<ulong>();

        await InsertBadgeIfNotAlreadyAsync(
                _clock.Now, userId, (ulong)badge, collectedBadges, allBadges);

        return collectedBadges.Count > 0;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Badge>> GetAllBadgesAsync(Languages language)
    {
        var dtos = await _badgeRepository
            .GetBadgesAsync(false);

        var badges = new List<Badge>(dtos.Count);
        foreach (var dto in dtos)
        {
            var b = await GetBadgeAsync(
                    dto.Id, dtos, language);
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
        Languages language,
        bool foundToday)
    {
        var isAllowedToSeeHiddenBadge = connectedUserId == userId;
        if (connectedUserId > 0 && !isAllowedToSeeHiddenBadge)
        {
            var userDto = await _userRepository
                .GetUserByIdAsync(connectedUserId);

            isAllowedToSeeHiddenBadge = userDto?.UserTypeId == (ulong)UserTypes.Administrator;
        }

        var badges = await _badgeRepository
           .GetBadgesAsync(true);

        var dtos = await _badgeRepository
            .GetUserBadgesAsync(userId);

        if (!foundToday)
        {
            dtos = dtos.Where(_ => _.GetDate.Date < _clock.Today).ToList();
        }

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
                    dto.BadgeId, badges, dto.GetDate, language);

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
            .Where(p => leadersHistory.Any(h => h.UserId == leader.UserId && h.ProposalDate == p.PublicationDate));

        var myCreatedPlayers = playersHistory
            .Where(p => p.CreationUserId == leader.UserId);

        var playerFull = await _playerHandler
            .GetPlayerFullInfoAsync(playerOfTheDay);

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
                            leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges);
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
                            leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges);
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
                            leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges);
                }
            }

            foreach (var badge in LeadersBasedBadgeCondition.Keys)
            {
                var hasBadgeNotAlone = LeadersBasedBadgeNonUniqueCondition[badge](leader, leaders);
                if (hasBadgeNotAlone)
                {
                    var badgeOwners = await _badgeRepository
                        .GetUsersOfTheDayWithBadgeAsync((ulong)badge, leader.ProposalDate);

                    foreach (var bo in badgeOwners)
                    {
                        await _badgeRepository
                            .RemoveUserBadgeAsync(new UserBadgeDto
                            {
                                BadgeId = (ulong)badge,
                                UserId = bo.UserId
                            });
                    }

                    if (LeadersBasedBadgeCondition[badge](leader, leaders))
                    {
                        await InsertBadgeIfNotAlreadyAsync(
                                 leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges);
                    }
                }
            }

            foreach (var badge in ProposalsBasedBadgeCondition.Keys)
            {
                if (ProposalsBasedBadgeCondition[badge](leader.CreationDate, playerFull, proposalsBeforeWin))
                {
                    await InsertBadgeIfNotAlreadyAsync(
                            leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges);
                }
            }
        }

        foreach (var badge in PlayerBasedBadgeCondition.Keys)
        {
            if (PlayerBasedBadgeCondition[badge](playerOfTheDay))
            {
                await InsertBadgeIfNotAlreadyAsync(
                         leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges);
            }
        }

        foreach (var badge in PlayersHistoryBasedBadgeCondition.Keys)
        {
            if (PlayersHistoryBasedBadgeCondition[badge](myPlayerHistory))
            {
                await InsertBadgeIfNotAlreadyAsync(
                        leader.ProposalDate, leader.UserId, (ulong)badge, collectedBadges, allBadges);
            }
        }

        if (playerOfTheDay.BadgeId.HasValue)
        {
            await InsertBadgeIfNotAlreadyAsync(
                    leader.ProposalDate, leader.UserId, playerOfTheDay.BadgeId.Value, collectedBadges, allBadges);
        }

        return await GetUserBadgesAsync(
                collectedBadges, leader.ProposalDate, allBadges, language);
    }

    private static bool RespectLeadersRunConditions(LeaderDto leader,
        IEnumerable<LeaderDto> myHistory,
        IEnumerable<PlayerDto> myCreatedPlayers,
        int runLength,
        Func<LeaderDto, bool> funcConditionOnLeader,
        bool creatorIncludeInRun)
    {
        return RespectLeadersRunConditionsInternal(leader, myHistory, myCreatedPlayers, runLength,
            funcConditionOnLeader, creatorIncludeInRun, 0, null, null);
    }

    private static bool RespectLeadersRunConditions(LeaderDto leader,
        IEnumerable<LeaderDto> myHistory,
        IEnumerable<PlayerDto> myCreatedPlayers,
        int initialValue,
        int runLength,
        Func<LeaderDto, int, int> aggFunc,
        Func<int, bool> checkFunc)
    {
        return RespectLeadersRunConditionsInternal(leader, myHistory, myCreatedPlayers, runLength,
            null, false, initialValue, aggFunc, checkFunc);
    }

    private static bool RespectLeadersRunConditionsInternal(LeaderDto leader,
        IEnumerable<LeaderDto> myHistory,
        IEnumerable<PlayerDto> myCreatedPlayers,
        int runLength,
        Func<LeaderDto, bool>? funcConditionOnLeader,
        bool creatorIncludeInRun,
        int initialValue,
        Func<LeaderDto, int, int>? aggFunc,
        Func<int, bool>? checkFunc)
    {
        var agg = initialValue;
        var i = 0;
        var dateToConsider = leader.ProposalDate;
        do
        {
            var isCreator = myCreatedPlayers.Any(mcp => mcp.PublicationDate == dateToConsider);

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
            .CheckUserHasBadgeAsync(userId, badge);

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
                    });

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
                    badge, allBadges, proposalDate, language);

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
                badge, badgesDto, language);
        return new UserBadge(b, proposalDate);
    }

    private async Task<Badge> GetBadgeAsync(
        ulong badge,
        IReadOnlyCollection<BadgeDto> badgesDto,
        Languages language)
    {
        string? description = null;

        if (language != Languages.en)
        {
            description = await _badgeRepository
                .GetBadgeDescriptionAsync(badge, (ulong)language);
        }

        var users = await _badgeRepository
            .GetUsersWithBadgeAsync(badge);

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
                .GetLeadersAtDateAsync(date, false);
            leadersHistory.AddRange(leadersBefore);
            date = date.AddDays(-1);
        }

        return leadersHistory;
    }
}
