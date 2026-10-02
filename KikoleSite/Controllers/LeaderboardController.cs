using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Controllers.Attributes;
using KikoleSite.Helpers;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KikoleSite.Controllers;

public class LeaderboardController : KikoleBaseController
{
    private const string AnonymizedPlayerName = "***";

    private readonly ILeaderService _leaderService;
    private readonly IProposalService _proposalService;

    public LeaderboardController(IUserRepository userRepository,
        IInternationalService internationalService,
        IClock clock,
        IGameCalendar gameCalendar,
        IPlayerService playerService,
        IBadgeService badgeService,
        ILeaderService leaderService,
        IProposalService proposalService,
        IHttpContextAccessor httpContextAccessor)
        : base(userRepository,
            internationalService,
            clock,
            gameCalendar,
            playerService,
            badgeService,
            httpContextAccessor)
    {
        _leaderService = leaderService;
        _proposalService = proposalService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ulong userId)
    {
        // /!\ userId is not UserId
        if (userId == 0)
        {
            var model = await InitializeModelAsync();
            return View(model);
        }

        var todayGrant = await _proposalService
            .GetGrantAccessForDayAsync(UserId, _clock.Today);

        var stats = await _leaderService
            .GetUserStatisticsAsync(userId, UserId, AnonymizedPlayerName, todayGrant != DayGrantTypes.None);

        if (stats == null)
        {
            var model = await InitializeModelAsync();
            return View(model);
        }

        var language = ViewHelper.GetLanguage();

        // les badges obtenus aujourd'hui par un autre joueur ne sont visibles que si on
        // connait deja la reponse du jour (trouve, createur, admin) : PaidBoard (classement
        // achete sans avoir trouve) ne suffit pas, un badge comme "OK Zoomer" ou
        // "Archaeology" trahirait l'annee de naissance du kikole du jour
        var knowsTodaysAnswer = todayGrant is DayGrantTypes.Found or DayGrantTypes.Creator or DayGrantTypes.Admin;

        var badges = await _badgeService
             .GetUserBadgesAsync(userId, UserId, language, knowsTodaysAnswer);

        var allBadges = await _badgeService
            .GetAllBadgesAsync(language);

        return View("User", new UserStatsModel(stats, badges, allBadges, userId == UserId, _clock));
    }

    [HttpGet("global-leaderboard-details")]
    public async Task<JsonResult> GetGlobalLeaderboardDetailsAsync(LeaderSorts sortType, DateOnly minimalDate, DateOnly maximalDate)
    {
        var (ld, _) = await GetLeaderboardAsync(
                minimalDate, maximalDate, sortType, null);

        return Json(ld.Select(LeaderboardRow.From).ToList());
    }

    [HttpGet("daily-leaderboard-details")]
    public async Task<JsonResult> GetDailyLeaderboardDetailsAsync(DayLeaderSorts sortType, DateOnly date)
    {
        var (dailyBoard, _) = await GetDailyboardAsync(
                date, sortType, null);

        return Json(DayboardModel.From(dailyBoard));
    }

    /// <summary>
    /// Achete l'acces au classement du jour depuis la page classement elle-meme (bouton
    /// "Decouvrez le classement" affiche quand le tableau du jour est masque) - meme
    /// mecanique que le bouton "Acces au classement du jour" de la page d'accueil
    /// (proposition de type <see cref="ProposalTypes.Leaderboard"/>, cout fixe), pour
    /// eviter d'avoir a retourner sur l'accueil. Idempotent : une deuxieme tentative le
    /// meme jour ne recree pas la proposition ni ne rededuit de points
    /// (<see cref="ProposalRequest.MatchAny"/>, verifie dans <see cref="IProposalService.ManageProposalResponseAsync"/>).
    /// </summary>
    [HttpPost("unlock-daily-leaderboard")]
    [Authorization]
    public async Task<JsonResult> UnlockDailyLeaderboardAsync()
    {
        var pInfo = await _playerService.GetPlayerOfTheDayFullInfoAsync(_clock.Today);
        var countryContinents = await _internationalService.GetCountryContinentsAsync();

        var request = new ProposalRequest
        {
            DaysBeforeNow = 0,
            ProposalDateTime = _clock.Now,
            Ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
            ProposalType = ProposalTypes.Leaderboard,
            Value = "GetLeaderboard"
        };

        await _proposalService.ManageProposalResponseAsync(request, UserId, pInfo, countryContinents);

        return Json(new { success = true });
    }

    [HttpGet]
    [Authorization]
    public async Task<IActionResult> UserDay(ulong userId, string date)
    {
        if (!DateOnly.TryParse(date, out var actualDate)
            || actualDate > _clock.Today
            || actualDate < _gameCalendar.HiddenDate)
        {
            return RedirectToAction("ErrorIndex", "Home");
        }

        var user = await _userRepository
            .GetUserByIdAsync(userId);
        if (user == null || user.UserTypeId == (int)UserTypes.Administrator)
            return RedirectToAction("ErrorIndex", "Home");

        var canSee = await _proposalService
            .GetGrantAccessForDayAsync(UserId, actualDate);

        if (canSee != DayGrantTypes.Creator && canSee != DayGrantTypes.Found && canSee != DayGrantTypes.Admin)
            return RedirectToAction("ErrorIndex", "Home");

        var player = await _playerService
            .GetPlayerOfTheDayFullInfoAsync(actualDate);

        if (player.Player.CreationUserId == userId)
            return RedirectToAction("ErrorIndex", "Home");

        var countryContinents = await _internationalService.GetCountryContinentsAsync();

        // toutes les gardes d'acces sont deja passees a ce stade : plus de travail a
        // eviter en cas de refus, db/proposals peuvent partir en // sans compromis
        var dbTask = _leaderService
            .GetDayboardAsync(actualDate, DayLeaderSorts.BestTime, countryContinents);

        var proposalsTask = _proposalService
            .GetProposalsAsync(actualDate, userId, countryContinents);

        var db = await dbTask;

        var proposals = await proposalsTask;

        var items = new List<UserDayItemModel>(proposals.Count);
        foreach (var proposal in proposals)
        {
            items.Add(new UserDayItemModel
            {
                Date = proposal.Date,
                PointsLost = proposal.PointsLost,
                PointsRemaining = proposal.TotalPoints,
                Success = proposal.Successful,
                Type = proposal.ProposalType,
                Value = proposal.RawValue
            });
        }

        // le dernier point restant si le classement n'a pas encore de score enregistre
        // pour ce jour (ex. abandon), sinon BasePoints faute de proposition du tout
        var lastKnownPoints = proposals.Count > 0
            ? proposals.Last().TotalPoints
            : ScoreCalculator.BasePoints;

        var (previousDate, nextDate) = await GetNeighbourPlayedDaysAsync(userId, actualDate);

        var model = new UserDayModel
        {
            ProposalDate = actualDate,
            PlayerName = player.Player.Name,
            UserLogin = user.Login,
            UserId = userId,
            ProposalDetails = items,
            UserScore = db.Leaders.FirstOrDefault(_ => _.UserId == userId)?.Points ?? lastKnownPoints,
            PreviousDate = previousDate,
            NextDate = nextDate
        };

        return View("UserDay", model);
    }

    /// <summary>
    /// Jours joués les plus proches (avant / après) de <paramref name="date"/> pour ce joueur,
    /// en ne gardant que ceux que le visiteur a le droit d'ouvrir : mêmes gardes d'accès que
    /// <see cref="UserDay"/>, sinon une flèche mènerait à une page d'erreur. Un jour où le
    /// joueur est créateur n'est pas un jour joué (aucune proposition), donc jamais proposé.
    /// </summary>
    private async Task<(DateOnly? Previous, DateOnly? Next)> GetNeighbourPlayedDaysAsync(ulong userId, DateOnly date)
    {
        var todayGrant = await _proposalService
            .GetGrantAccessForDayAsync(UserId, _clock.Today);

        var stats = await _leaderService
            .GetUserStatisticsAsync(userId, UserId, AnonymizedPlayerName, todayGrant != DayGrantTypes.None);

        if (stats == null)
            return (null, null);

        var playedDays = stats.Stats
            .Where(s => s.Attempt)
            .Select(s => s.Date)
            .Distinct()
            .ToList();

        var previous = await FirstViewableDayAsync(
            playedDays.Where(d => d < date).OrderByDescending(d => d));

        var next = await FirstViewableDayAsync(
            playedDays.Where(d => d > date).OrderBy(d => d));

        return (previous, next);
    }

    private async Task<DateOnly?> FirstViewableDayAsync(IEnumerable<DateOnly> candidates)
    {
        foreach (var candidate in candidates)
        {
            var grant = await _proposalService
                .GetGrantAccessForDayAsync(UserId, candidate);

            if (grant is DayGrantTypes.Creator or DayGrantTypes.Found or DayGrantTypes.Admin)
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Modele par defaut du classement : le mois courant, trie par points cumules.
    /// </summary>
    private async Task<LeaderboardModel> InitializeModelAsync()
    {
        // le classement general depend de "foundToday" (issu du dayboard) : seul le
        // dayboard et les podiums, independants l'un de l'autre, peuvent partir en //
        var dayboardTask = GetDailyboardAsync(_clock.Today, DayLeaderSorts.BestTime, null);
        var podiumsTask = _leaderService.GetPodiumsAsync();

        var (dailyBoard, foundToday) = await dayboardTask;

        var (globalLeaderboard, _) = await GetLeaderboardAsync(
                _clock.FirstOfMonth, _clock.Today, LeaderSorts.TotalPoints, foundToday);

        var podiums = await podiumsTask;

        return new LeaderboardModel
        {
            MinimalDate = _clock.FirstOfMonth,
            MaximalDate = _clock.Today,
            SortType = LeaderSorts.TotalPoints,
            LeaderboardDay = _clock.Today,
            DaySortType = DayLeaderSorts.BestTime,
            Dayboard = DayboardModel.From(dailyBoard),
            GlobalLeaderboard = globalLeaderboard.Select(LeaderboardRow.From).ToList(),
            CurrentUserId = UserId,
            MonthlyPodiums = podiums.MonthlyPodiums
                .Select(x => (
                    new DateOnly(x.Key.year, x.Key.month, 1),
                    new[]
                    {
                        (x.Value.first.Id, x.Value.first.Login),
                        (x.Value.second.Id, x.Value.second.Login),
                        (x.Value.third.Id, x.Value.third.Login)
                    }))
                .ToList(),
            OverallPodium = podiums.OverallPodium
                .Select(x => (x.user.Id, x.user.Login, x.first, x.second, x.third))
                .ToList()
        };
    }

    private async Task<(IReadOnlyCollection<Models.LeaderboardItem>, DayGrantTypes)> GetLeaderboardAsync(
        DateOnly minDate, DateOnly maxDate, LeaderSorts sortType, DayGrantTypes? todayGrant)
    {
        // cumul sur toute la partie, aucune notion de periode ni de spoil du jour (le %
        // de badges ne revele rien sur le kikole du jour) : ni la garde d'acces "today",
        // ni le bornage de dates ci-dessous ne s'appliquent a ce tri.
        if (sortType == LeaderSorts.BadgePercentage)
        {
            var badgeBoard = await _leaderService
                .GetLeaderboardAsync(minDate, maxDate, sortType);
            return (badgeBoard, todayGrant ?? DayGrantTypes.None);
        }

        var todayGrantEnsured = todayGrant ?? await _proposalService
            .GetGrantAccessForDayAsync(UserId, _clock.Today);

        // this case usually happens the first of the month
        // the former code switch to the previous month
        if (todayGrantEnsured == DayGrantTypes.None
            && minDate >= _clock.Today
            && maxDate >= _clock.Today)
        {
            return (new List<Models.LeaderboardItem>(), todayGrantEnsured);
        }

        // independants l'un de l'autre : partent en //
        var minDateTask = EnsureDateAsync(minDate, todayGrantEnsured);
        var maxDateTask = EnsureDateAsync(maxDate, todayGrantEnsured);

        minDate = await minDateTask;
        maxDate = await maxDateTask;

        if (maxDate < minDate)
        {
            var swap = minDate;
            minDate = maxDate;
            maxDate = swap;
        }

        var board = await _leaderService
            .GetLeaderboardAsync(minDate, maxDate, sortType);

        return (board, todayGrantEnsured);
    }

    private async Task<(Models.Dayboard, DayGrantTypes)> GetDailyboardAsync(
        DateOnly date, DayLeaderSorts sortType, DayGrantTypes? todayGrant)
    {
        // EnsureDateAsync ci-dessous recoit une valeur figee (DayGrantTypes.Found), pas
        // todayGrant : les deux sont donc independants et partent en //
        var todayGrantTask = todayGrant.HasValue
            ? Task.FromResult(todayGrant.Value)
            : _proposalService.GetGrantAccessForDayAsync(UserId, _clock.Today);

        var dateTask = EnsureDateAsync(date, DayGrantTypes.Found); // any DayGrantTypes but "None"

        var todayGrantEnsured = await todayGrantTask;

        date = await dateTask;

        Dayboard dayboard;
        if (date == _clock.Today && todayGrantEnsured == DayGrantTypes.None)
        {
            dayboard = new Dayboard
            {
                Date = date,
                Sort = sortType,
                Hidden = true,
                // le tableau est masque : collections vides plutot que nulles, pour
                // qu'un appelant qui oublierait de tester Hidden ne casse pas
                Leaders = [],
                Searchers = []
            };
        }
        else
        {
            dayboard = await _leaderService
                .GetDayboardAsync(date, sortType, await _internationalService.GetCountryContinentsAsync());
        }

        // le grant "today" ci-dessus ne sert qu'a decider si LE TABLEAU du jour doit
        // etre masque (cf. ci-dessus) - le detail par utilisateur (lien vers UserDay)
        // est une autorisation distincte, propre au jour reellement affiche : un
        // classement achete (PaidBoard) ou un jour passe jamais joue laisse voir le
        // tableau mais pas le detail (meme regle que la garde de l'action UserDay).
        var dayGrant = date == _clock.Today
            ? todayGrantEnsured
            : await _proposalService.GetGrantAccessForDayAsync(UserId, date);
        dayboard.CanViewDetails = dayGrant is DayGrantTypes.Found or DayGrantTypes.Creator or DayGrantTypes.Admin;

        return (dayboard, todayGrantEnsured);
    }

    private async Task<DateOnly> EnsureDateAsync(DateOnly date, DayGrantTypes todayGrant)
    {
        if (date > _clock.Today)
        {
            date = _clock.Today;
        }

        if (todayGrant == DayGrantTypes.None && date == _clock.Today)
        {
            date = _clock.Yesterday;
        }

        if (date <= _gameCalendar.HiddenDate)
        {
            date = _gameCalendar.HiddenDate;
            var displayHidden = await _playerService
                .CanDisplayHiddenPlayerAsync(UserId);
            if (!displayHidden)
                date = _gameCalendar.FirstDate;
        }

        return date;
    }
}
