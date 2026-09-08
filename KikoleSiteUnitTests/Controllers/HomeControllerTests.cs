using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KikoleSite;
using KikoleSite.Configuration;
using KikoleSite.Controllers;
using KikoleSite.Helpers;
using KikoleSite.Identity;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Controllers;

/// <summary>
/// Couvre <see cref="HomeController.Index(int?, string)"/> (GET) et
/// <see cref="HomeController.Index(HomeModel)"/> (POST), jusqu'ici hors perimetre (cf.
/// TODO) faute d'un moyen etabli de mocker <see cref="Microsoft.AspNetCore.Identity.SignInManager{TUser}"/>
/// (voir <see cref="IdentityMocks"/>). Ecrit en filet de regression avant de paralleliser
/// certains appels independants de <c>SetAndGetViewModelAsync</c> (playerCreator/clue/
/// easyClue/streak, puis proposals/countries/continents/clubs) et de deduire une seule
/// fois countries/continents au lieu de les rappeler deux fois (branche proposition +
/// bloc final) : les scenarios ci-dessous fixent le comportement observable actuel,
/// independant de savoir si ces appels partent en sequence ou en parallele.
/// </summary>
public class HomeControllerTests
{
    private static readonly DateTime Today = TestCalendar.FirstDate.AddDays(30);
    private static readonly Languages Lang = KikoleSite.Helpers.ViewHelper.GetLanguage();

    private readonly DefaultHttpContext _httpContext = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IInternationalService> _internationalService = new();
    private readonly Mock<IMessageRepository> _messageRepository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly Mock<IPlayerService> _playerService = new();
    private readonly Mock<IProposalService> _proposalService = new();
    private readonly Mock<ILeaderService> _leaderService = new();
    private readonly Mock<IBadgeService> _badgeService = new();
    private readonly Mock<IDiscussionService> _discussionService = new();
    private readonly Mock<IStringLocalizer<HomeController>> _localizer = new();
    private readonly Mock<Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser>> _signInManager;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _signInManager = IdentityMocks.MockSignInManager(IdentityMocks.MockUserManager());

        _clock.Setup(_ => _.Today).Returns(Today);
        _clock.Setup(_ => _.Now).Returns(Today);

        _localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));
        _localizer.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((k, args) => new LocalizedString(k, k));

        _messageRepository.Setup(_ => _.GetMessageAsync(It.IsAny<DateTime>())).ReturnsAsync((MessageDto?)null);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);

        _controller = new HomeController(
            _localizer.Object,
            _userRepository.Object,
            _internationalService.Object,
            _messageRepository.Object,
            _clock.Object,
            _gameCalendar.Object,
            _playerService.Object,
            _proposalService.Object,
            _leaderService.Object,
            _badgeService.Object,
            _discussionService.Object,
            _signInManager.Object,
            Options.Create(new RegistrationOptions()),
            httpContextAccessor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    private void SetUser(ulong? userId, string? login, UserTypes? userType)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        if (userType.HasValue)
            claims.Add(new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)userType.Value).ToString()));

        var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null);
        if (login != null)
            identity.AddClaim(new Claim(ClaimTypes.Name, login));

        _httpContext.User = new ClaimsPrincipal(identity);
    }

    private void SetFormKeys(params string[] keys)
    {
        var dict = new Dictionary<string, StringValues>();
        foreach (var key in keys)
            dict[key] = StringValues.Empty;
        _httpContext.Request.Form = new FormCollection(dict);
    }

    /// <summary>Contexte pays/continent partage par les scenarios "joueur trouve/createur".</summary>
    private void SetupCountryContinents()
    {
        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);
        _internationalService.Setup(_ => _.GetCountriesAsync(Lang))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Countries.FRA, "France" } });
        _internationalService.Setup(_ => _.GetContinentsAsync(Lang))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Continents.Europe, "Europe" } });
        _internationalService.Setup(_ => _.GetClubsAsync()).ReturnsAsync(Array.Empty<Club>());
    }

    private void SetupClues()
    {
        _playerService.Setup(_ => _.GetPlayerClueAsync(Today, false, Lang)).ReturnsAsync("un indice");
        _playerService.Setup(_ => _.GetPlayerClueAsync(Today, true, Lang)).ReturnsAsync("un indice facile");
    }

    private static ProposalResponse BuildResponse(ProposalDto dto, PlayerFullDto player)
    {
        var resources = new Mock<IStringLocalizer>();
        resources.Setup(r => r[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));
        return new ProposalResponse(dto, player, resources.Object, TestCountryContinents.Map);
    }

    // ------------------------------------------------------------- Index (GET)

    [Fact]
    public async Task IndexGet_AnonymousVisitor_SkipsPersonalizedData()
    {
        SetUser(userId: null, login: null, userType: null);
        SetupCountryContinents();
        SetupClues();

        var result = await _controller.Index(day: null, errorMessageForced: null!);

        var model = ((ViewResult)result).Model.Should().BeOfType<HomeModel>().Subject;
        model.LoggedAs.Should().BeNull();
        model.Streak.Should().BeNull();
        model.PlayerName.Should().BeNullOrEmpty();
        _playerService.Verify(_ => _.GetPlayerOfTheDayFromUserPovAsync(It.IsAny<ulong>(), It.IsAny<DateTime>()), Times.Never);
        _leaderService.Verify(_ => _.GetUserStreakAsync(It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task IndexGet_LoggedInNotCreatorNotFound_PopulatesClueAndStreak()
    {
        SetUser(userId: 7, login: "joueur1", userType: UserTypes.StandardUser);
        SetupCountryContinents();
        SetupClues();

        var requestUser = UserDtoBuilder.Valid().WithId(7).WithType(UserTypes.StandardUser).Build();
        var playerDto = PlayerDtoBuilder.Valid().Build(); // CreationUserId = 42, different from 7
        var creatorUser = UserDtoBuilder.Valid().WithId(42).Build();
        _playerService
            .Setup(_ => _.GetPlayerOfTheDayFromUserPovAsync(7, Today))
            .ReturnsAsync(new PlayerCreator(requestUser, playerDto, creatorUser));

        _proposalService
            .Setup(_ => _.GetProposalsAsync(Today, 7, TestCountryContinents.Map))
            .ReturnsAsync(Array.Empty<ProposalResponse>());

        _leaderService
            .Setup(_ => _.GetUserStreakAsync(7))
            .ReturnsAsync(new UserStreak { Current = 3, Best = 5 });

        var result = await _controller.Index(day: null, errorMessageForced: null!);

        var model = ((ViewResult)result).Model.Should().BeOfType<HomeModel>().Subject;
        model.LoggedAs.Should().Be("joueur1");
        model.Clue.Should().Be("un indice");
        model.PlayerName.Should().BeNullOrEmpty();
        model.Streak.Should().NotBeNull();
        model.Streak!.Current.Should().Be(3);
        model.Streak.Best.Should().Be(5);
    }

    [Fact]
    public async Task IndexGet_Creator_PopulatesFinalBlockFromCountriesAndContinents()
    {
        SetUser(userId: 42, login: "createur", userType: UserTypes.StandardUser);
        SetupCountryContinents();
        SetupClues();

        var requestUser = UserDtoBuilder.Valid().WithId(42).WithType(UserTypes.StandardUser).Build();
        var playerDto = PlayerDtoBuilder.Valid().WithCreator(42).Build();
        _playerService
            .Setup(_ => _.GetPlayerOfTheDayFromUserPovAsync(42, Today))
            .ReturnsAsync(new PlayerCreator(requestUser, playerDto, requestUser));

        var playerFull = PlayerFullDtoBuilder.Valid().WithPlayer(playerDto).WithCareer((1, "Real Madrid", 1)).Build();
        _playerService.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Today)).ReturnsAsync(playerFull);

        _leaderService.Setup(_ => _.GetUserStreakAsync(42)).ReturnsAsync(new UserStreak { Current = 0, Best = 0 });

        var result = await _controller.Index(day: null, errorMessageForced: null!);

        var model = ((ViewResult)result).Model.Should().BeOfType<HomeModel>().Subject;
        model.IsCreator.Should().BeTrue();
        model.PlayerName.Should().Be("Zinédine Zidane");
        model.CountryName.Should().Be("France");
        model.ContinentName.Should().Be("Europe");
        model.Position.Should().Be(Positions.Midfielder.GetLabel());
        model.BirthYear.Should().Be("1972");
        model.KnownPlayerClubs.Should().ContainSingle(c => c.Name == "Real Madrid");

        // le createur ne passe jamais par la branche "proposals" : un seul appel a
        // chaque referentiel (celui du bloc final), pas un doublon a dedupliquer ici
        _internationalService.Verify(_ => _.GetCountriesAsync(Lang), Times.Once);
        _internationalService.Verify(_ => _.GetContinentsAsync(Lang), Times.Once);
    }

    [Fact]
    public async Task IndexGet_FoundPlayerViaProposal_BothBlocksAgreeOnCountryAndContinent()
    {
        // scenario cle : le joueur vient d'etre trouve (branche "proposals" du else),
        // ET le bloc final s'execute juste apres (PlayerName non vide) - countries/
        // continents sont aujourd'hui rappeles une seconde fois pour ce bloc ; ce test
        // doit rester vert une fois les deux fusionnes en un seul appel partage.
        SetUser(userId: 7, login: "joueur1", userType: UserTypes.StandardUser);
        SetupCountryContinents();
        SetupClues();

        var requestUser = UserDtoBuilder.Valid().WithId(7).WithType(UserTypes.StandardUser).Build();
        var playerDto = PlayerDtoBuilder.Valid().Build(); // CreationUserId = 42 != 7
        var creatorUser = UserDtoBuilder.Valid().WithId(42).Build();
        _playerService
            .Setup(_ => _.GetPlayerOfTheDayFromUserPovAsync(7, Today))
            .ReturnsAsync(new PlayerCreator(requestUser, playerDto, creatorUser));

        var playerFull = PlayerFullDtoBuilder.Valid().WithPlayer(playerDto).WithCareer((1, "Real Madrid", 1)).Build();
        _playerService.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Today)).ReturnsAsync(playerFull);

        var winningProposal = BuildResponse(
            ProposalDtoBuilder.Valid().OfType(ProposalTypes.Name).WithSuccessfulFlag(1).WithCreationDate(Today).Build(),
            playerFull);
        _proposalService
            .Setup(_ => _.GetProposalsAsync(Today, 7, TestCountryContinents.Map))
            .ReturnsAsync(new[] { winningProposal });

        _leaderService.Setup(_ => _.GetUserStreakAsync(7)).ReturnsAsync(new UserStreak { Current = 1, Best = 1 });

        var result = await _controller.Index(day: null, errorMessageForced: null!);

        var model = ((ViewResult)result).Model.Should().BeOfType<HomeModel>().Subject;
        model.PlayerName.Should().Be("Zinédine Zidane");
        model.FoundOnTime.Should().BeTrue();
        model.CountryName.Should().Be("France");
        model.ContinentName.Should().Be("Europe");
        model.Position.Should().Be(Positions.Midfielder.GetLabel());
        model.BirthYear.Should().Be("1972");
        model.KnownPlayerClubs.Should().ContainSingle(c => c.Name == "Real Madrid");
    }

    // ------------------------------------------------------------- Index (POST)

    [Fact]
    public async Task IndexPost_NullModel_RedirectsToRoot()
    {
        var result = await _controller.Index((HomeModel)null!);

        result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("/");
    }

    [Fact]
    public async Task IndexPost_UnparsableSubmitAction_RedirectsToRoot()
    {
        SetUser(userId: 7, login: "joueur1", userType: UserTypes.StandardUser);
        SetFormKeys("submit-Bogus");

        var result = await _controller.Index(new HomeModel());

        result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("/");
        _proposalService.Verify(_ => _.ManageProposalResponseAsync(
            It.IsAny<KikoleSite.Models.Requests.ProposalRequest>(), It.IsAny<ulong>(), It.IsAny<PlayerFullDto>(), It.IsAny<IReadOnlyDictionary<ulong, ulong>>()),
            Times.Never);
    }

    [Fact]
    public async Task IndexPost_InvalidInputValue_RecursesIntoGetWithForcedError()
    {
        SetUser(userId: null, login: null, userType: null);
        SetupCountryContinents();
        SetupClues();
        SetFormKeys("submit-Continent");

        var model = new HomeModel { ContinentNameSubmission = "PasUnContinent" };

        var result = await _controller.Index(model);

        var renderedModel = ((ViewResult)result).Model.Should().BeOfType<HomeModel>().Subject;
        renderedModel.IsErrorMessageForced.Should().BeTrue();
        renderedModel.MessageToDisplay.Should().Be("InvalidRequest");
        _playerService.Verify(_ => _.GetPlayerOfTheDayFullInfoAsync(It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task IndexPost_ValidNonWinningProposal_OnlyCallsNonLeaderBadges()
    {
        SetUser(userId: 7, login: "joueur1", userType: UserTypes.StandardUser);
        SetupCountryContinents();
        SetupClues();
        SetFormKeys("submit-Continent");

        var playerFull = PlayerFullDtoBuilder.Valid().Build();
        _playerService.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Today)).ReturnsAsync(playerFull);

        var requestUser = UserDtoBuilder.Valid().WithId(7).WithType(UserTypes.StandardUser).Build();
        _playerService
            .Setup(_ => _.GetPlayerOfTheDayFromUserPovAsync(7, Today))
            .ReturnsAsync(new PlayerCreator(requestUser, playerFull.Player, UserDtoBuilder.Valid().WithId(42).Build()));
        _proposalService.Setup(_ => _.GetProposalsAsync(Today, 7, TestCountryContinents.Map)).ReturnsAsync(Array.Empty<ProposalResponse>());
        _leaderService.Setup(_ => _.GetUserStreakAsync(7)).ReturnsAsync(new UserStreak { Current = 0, Best = 0 });

        var response = BuildResponse(
            ProposalDtoBuilder.Valid().OfType(ProposalTypes.Continent).WithSuccessfulFlag(0).WithValue("Europe").Build(),
            playerFull);

        _proposalService
            .Setup(_ => _.ManageProposalResponseAsync(
                It.IsAny<KikoleSite.Models.Requests.ProposalRequest>(), 7, playerFull, TestCountryContinents.Map))
            .ReturnsAsync((response, (IReadOnlyCollection<ProposalDto>)Array.Empty<ProposalDto>(), (LeaderDto?)null));

        _badgeService
            .Setup(_ => _.PrepareNonLeaderBadgesAsync(7, It.IsAny<KikoleSite.Models.Requests.ProposalRequest>(), Lang))
            .ReturnsAsync(Array.Empty<UserBadge>());

        var model = new HomeModel { ContinentNameSubmission = "Europe" };

        var result = await _controller.Index(model);

        var renderedModel = ((ViewResult)result).Model.Should().BeOfType<HomeModel>().Subject;
        renderedModel.JustWon.Should().BeFalse();
        _badgeService.Verify(_ => _.PrepareNewLeaderBadgesAsync(
            It.IsAny<LeaderDto>(), It.IsAny<PlayerDto>(), It.IsAny<IReadOnlyCollection<ProposalDto>>(), It.IsAny<Languages>()),
            Times.Never);
        _badgeService.Verify(_ => _.PrepareNonLeaderBadgesAsync(7, It.IsAny<KikoleSite.Models.Requests.ProposalRequest>(), Lang), Times.Once);

        // le fetch de pInfo/countryContinents (item a paralleliser) doit rester a un
        // seul appel chacun, qu'il parte en sequence ou via Task.WhenAll
        _playerService.Verify(_ => _.GetPlayerOfTheDayFullInfoAsync(Today), Times.Once);
        _internationalService.Verify(_ => _.GetCountryContinentsAsync(), Times.Once);
    }

    [Fact]
    public async Task IndexPost_ValidWinningProposal_AlsoCallsLeaderBadges()
    {
        SetUser(userId: 7, login: "joueur1", userType: UserTypes.StandardUser);
        SetupCountryContinents();
        SetupClues();
        SetFormKeys("submit-Continent");

        var playerFull = PlayerFullDtoBuilder.Valid().Build();
        _playerService.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Today)).ReturnsAsync(playerFull);

        var requestUser = UserDtoBuilder.Valid().WithId(7).WithType(UserTypes.StandardUser).Build();
        _playerService
            .Setup(_ => _.GetPlayerOfTheDayFromUserPovAsync(7, Today))
            .ReturnsAsync(new PlayerCreator(requestUser, playerFull.Player, UserDtoBuilder.Valid().WithId(42).Build()));
        _proposalService.Setup(_ => _.GetProposalsAsync(Today, 7, TestCountryContinents.Map)).ReturnsAsync(Array.Empty<ProposalResponse>());
        _leaderService.Setup(_ => _.GetUserStreakAsync(7)).ReturnsAsync(new UserStreak { Current = 1, Best = 1 });

        var response = BuildResponse(
            ProposalDtoBuilder.Valid().OfType(ProposalTypes.Continent).WithSuccessfulFlag(1).WithValue(((ulong)Continents.Europe).ToString()).Build(),
            playerFull);

        var leader = LeaderDtoBuilder.Valid().WithUserId(7).WithProposalDate(Today).Build();
        var proposalsAlready = (IReadOnlyCollection<ProposalDto>)Array.Empty<ProposalDto>();

        _proposalService
            .Setup(_ => _.ManageProposalResponseAsync(
                It.IsAny<KikoleSite.Models.Requests.ProposalRequest>(), 7, playerFull, TestCountryContinents.Map))
            .ReturnsAsync((response, proposalsAlready, leader));

        _badgeService
            .Setup(_ => _.PrepareNewLeaderBadgesAsync(leader, playerFull.Player, proposalsAlready, Lang))
            .ReturnsAsync(Array.Empty<UserBadge>());
        _badgeService
            .Setup(_ => _.PrepareNonLeaderBadgesAsync(7, It.IsAny<KikoleSite.Models.Requests.ProposalRequest>(), Lang))
            .ReturnsAsync(Array.Empty<UserBadge>());

        var model = new HomeModel { ContinentNameSubmission = "Europe" };

        var result = await _controller.Index(model);

        var renderedModel = ((ViewResult)result).Model.Should().BeOfType<HomeModel>().Subject;
        renderedModel.JustWon.Should().BeTrue();
        _badgeService.Verify(_ => _.PrepareNewLeaderBadgesAsync(leader, playerFull.Player, proposalsAlready, Lang), Times.Once);
    }

    [Fact]
    public async Task IndexPost_GiveUp_NeverCallsBadgesAndRevealsTheRealName()
    {
        SetUser(userId: 7, login: "joueur1", userType: UserTypes.StandardUser);
        SetupCountryContinents();
        SetupClues();
        SetFormKeys("submit-GiveUp");

        var playerFull = PlayerFullDtoBuilder.Valid().Build();
        _playerService.Setup(_ => _.GetPlayerOfTheDayFullInfoAsync(Today)).ReturnsAsync(playerFull);

        var requestUser = UserDtoBuilder.Valid().WithId(7).WithType(UserTypes.StandardUser).Build();
        _playerService
            .Setup(_ => _.GetPlayerOfTheDayFromUserPovAsync(7, Today))
            .ReturnsAsync(new PlayerCreator(requestUser, playerFull.Player, UserDtoBuilder.Valid().WithId(42).Build()));
        _proposalService.Setup(_ => _.GetProposalsAsync(Today, 7, TestCountryContinents.Map)).ReturnsAsync(Array.Empty<ProposalResponse>());
        _leaderService.Setup(_ => _.GetUserStreakAsync(7)).ReturnsAsync(new UserStreak { Current = 0, Best = 0 });

        // deja a 0 point : la boucle "do/while" s'arrete des la premiere iteration
        var zeroPointsResponse = BuildResponse(
            ProposalDtoBuilder.Valid().OfType(ProposalTypes.Name).WithSuccessfulFlag(0).Build(), playerFull)
            .WithTotalPoints(0, false);
        var revealResponse = BuildResponse(
            ProposalDtoBuilder.Valid().OfType(ProposalTypes.Name).WithSuccessfulFlag(1).WithCreationDate(Today).Build(), playerFull);

        var emptyProposals = (IReadOnlyCollection<ProposalDto>)Array.Empty<ProposalDto>();
        _proposalService
            .SetupSequence(_ => _.ManageProposalResponseAsync(
                It.IsAny<KikoleSite.Models.Requests.ProposalRequest>(), 7, playerFull, TestCountryContinents.Map))
            .ReturnsAsync((zeroPointsResponse, emptyProposals, (LeaderDto?)null))
            .ReturnsAsync((revealResponse, emptyProposals, (LeaderDto?)null));

        var model = new HomeModel();

        var result = await _controller.Index(model);

        result.Should().BeOfType<ViewResult>();
        _proposalService.Verify(_ => _.ManageProposalResponseAsync(
            It.IsAny<KikoleSite.Models.Requests.ProposalRequest>(), 7, playerFull, TestCountryContinents.Map),
            Times.Exactly(2));
        _proposalService.Verify(_ => _.ManageProposalResponseAsync(
            It.Is<KikoleSite.Models.Requests.ProposalRequest>(r => r.Value == playerFull.Player.Name), 7, playerFull, TestCountryContinents.Map),
            Times.Once);
        _badgeService.Verify(_ => _.PrepareNonLeaderBadgesAsync(
            It.IsAny<ulong>(), It.IsAny<KikoleSite.Models.Requests.ProposalRequest>(), It.IsAny<Languages>()), Times.Never);
        _badgeService.Verify(_ => _.PrepareNewLeaderBadgesAsync(
            It.IsAny<LeaderDto>(), It.IsAny<PlayerDto>(), It.IsAny<IReadOnlyCollection<ProposalDto>>(), It.IsAny<Languages>()), Times.Never);
    }

    // ------------------------------------------------------------- Contact

    [Fact]
    public async Task ContactGet_AsAdministrator_RedirectsToAdminDiscussions()
    {
        SetUser(userId: 1, login: "admin", userType: UserTypes.Administrator);

        var result = await _controller.Contact();

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "Discussions" && r.ControllerName == "Admin");
        _discussionService.Verify(_ => _.GetOwnThreadAsync(It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task ContactGet_RequestAccess_PrefillsThePowerUserRequestMessage()
    {
        SetUser(userId: 7, login: "joueur1", userType: UserTypes.StandardUser);
        var thread = Array.Empty<DiscussionMessageDto>();
        _discussionService.Setup(_ => _.GetOwnThreadAsync(7)).ReturnsAsync(thread);

        var result = await _controller.Contact(requestAccess: true);

        var model = ((ViewResult)result).Model.Should().BeOfType<ContactModel>().Subject;
        model.NewMessage.Should().Be("RequestPowerUserMessage");
        model.Messages.Should().BeSameAs(thread);
    }

    [Fact]
    public async Task ContactPost_EmptyMessage_SetsErrorAndDoesNotPost()
    {
        SetUser(userId: 7, login: "joueur1", userType: UserTypes.StandardUser);
        _discussionService.Setup(_ => _.GetOwnThreadAsync(7)).ReturnsAsync(Array.Empty<DiscussionMessageDto>());

        var result = await _controller.Contact(new ContactModel { NewMessage = "   " });

        var model = ((ViewResult)result).Model.Should().BeOfType<ContactModel>().Subject;
        model.ErrorMessage.Should().Be("InvalidMessage");
        _discussionService.Verify(_ => _.PostUserMessageAsync(It.IsAny<ulong>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ContactPost_ValidMessage_PostsAndRefreshesTheThread()
    {
        SetUser(userId: 7, login: "joueur1", userType: UserTypes.StandardUser);
        var refreshed = new[] { new DiscussionMessageDto { Id = 1, DiscussionId = 3, Message = "salut" } };
        _discussionService.Setup(_ => _.GetOwnThreadAsync(7)).ReturnsAsync(refreshed);

        var result = await _controller.Contact(new ContactModel { NewMessage = "un message" });

        _discussionService.Verify(_ => _.PostUserMessageAsync(7, "un message"), Times.Once);
        var model = ((ViewResult)result).Model.Should().BeOfType<ContactModel>().Subject;
        model.Messages.Should().BeSameAs(refreshed);
        model.NewMessage.Should().BeNull();
    }

    // ------------------------------------------------------------- Error / ErrorIndex / SwitchLang

    [Fact]
    public async Task Error_SignsOutAndRedirectsToHome()
    {
        var result = await _controller.Error();

        _signInManager.Verify(_ => _.SignOutAsync(), Times.Once);
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "Index" && r.ControllerName == "Home");
    }

    [Fact]
    public async Task ErrorIndex_RendersHomeWithTheAuthenticationRequiredMessage()
    {
        SetUser(userId: null, login: null, userType: null);
        SetupCountryContinents();
        SetupClues();

        var result = await _controller.ErrorIndex();

        var model = ((ViewResult)result).Model.Should().BeOfType<HomeModel>().Subject;
        model.IsErrorMessageForced.Should().BeTrue();
        model.MessageToDisplay.Should().Be("AuthenticationRequired");
    }

    [Fact]
    public void SwitchLang_NoExistingCookie_SwitchesToEnglishAndRedirects()
    {
        var result = _controller.SwitchLang("/leaderboard");

        result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("/leaderboard");
        var setCookie = Uri.UnescapeDataString(_httpContext.Response.Headers["Set-Cookie"].ToString());
        setCookie.Should().Contain("uic=en");
    }
}
