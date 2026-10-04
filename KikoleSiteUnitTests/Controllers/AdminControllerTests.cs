using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KikoleSite;
using KikoleSite.Controllers;
using KikoleSite.Identity;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Controllers;

/// <summary>
/// Couvre <c>AdminController.PlayerSubmission</c> (GET) et le garde-fou "plus rien a
/// valider" partage par <c>AcceptPlayer</c>/<c>RefusePlayer</c>/<c>ChoosePlayer</c>, seuls
/// points d'entree de <c>GetPlayerSubmissionsList</c> - la methode privee dont
/// <c>countries</c>/<c>continents</c> vont etre parallelises (cf. TODO). Jusqu'ici hors
/// perimetre, comme le reste du controleur (cf. TODO).
/// </summary>
public class AdminControllerTests : IDisposable
{
    private static readonly DateOnly Today = TestCalendar.FirstDate.AddDays(30);

    private readonly DefaultHttpContext _httpContext = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IInternationalService> _internationalService = new();
    private readonly Mock<IMessageRepository> _messageRepository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly Mock<IPlayerService> _playerService = new();
    private readonly Mock<IBadgeService> _badgeService = new();
    private readonly Mock<ILeaderService> _leaderService = new();
    private readonly Mock<IDiscussionService> _discussionService = new();
    private readonly Mock<IStringLocalizer<AdminController>> _localizer = new();
    private readonly Mock<IWebHostEnvironment> _webHostEnvironment = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager = IdentityMocks.MockUserManager();
    private readonly Mock<IEmailProtector> _emailProtector = new();
    private readonly string _webRootPath = Path.Combine(Path.GetTempPath(), "kikole-tests-" + Guid.NewGuid());
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _clock.Setup(_ => _.Today).Returns(Today);
        _clock.Setup(_ => _.Now).Returns(Today.ToDateTime(TimeOnly.MinValue));
        _localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));
        _localizer.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((k, args) => new LocalizedString(k, k + ":" + string.Join(",", args)));
        _emailProtector.Setup(_ => _.Decrypt(It.IsAny<string>())).Returns<string>(c => "plain-" + c);
        _userRepository
            .Setup(_ => _.SearchUsersAsync(It.IsAny<string?>(), It.IsAny<UserStatusFilter>(), It.IsAny<UserTypes?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<KikoleSite.Models.Dtos.UserDto>)new List<KikoleSite.Models.Dtos.UserDto>(), 0));
        _webHostEnvironment.Setup(_ => _.WebRootPath).Returns(_webRootPath);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);

        _controller = new AdminController(
            _localizer.Object,
            _userRepository.Object,
            _internationalService.Object,
            _messageRepository.Object,
            _clock.Object,
            _gameCalendar.Object,
            _playerService.Object,
            _badgeService.Object,
            _leaderService.Object,
            _discussionService.Object,
            httpContextAccessor.Object,
            _webHostEnvironment.Object,
            _userManager.Object,
            _emailProtector.Object,
            NullLogger<AdminController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)UserTypes.Administrator).ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null));
    }

    // le repertoire jetable qui accueille les uploads (UploadClueMedia) pendant les
    // tests n'a rien a voir avec le vrai wwwroot ; nettoye a chaque test pour ne pas
    // laisser trainer de fichiers dans le repertoire temp de la machine
    public void Dispose()
    {
        if (Directory.Exists(_webRootPath))
            Directory.Delete(_webRootPath, recursive: true);
    }

    [Fact]
    public async Task PlayerSubmission_Get_MapsCountryAndContinentNamesFromTheReferential()
    {
        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);
        _internationalService.Setup(_ => _.GetCountriesAsync(It.IsAny<Languages>()))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Countries.FRA, "France" } });
        _internationalService.Setup(_ => _.GetContinentsAsync(It.IsAny<Languages>()))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Continents.Europe, "Europe" } });

        var playerFull = PlayerFullDtoBuilder.Valid().Build();
        var creator = UserDtoBuilder.Valid().WithId(playerFull.Player.CreationUserId).WithLogin("createur").Build();
        var submittedPlayer = new Player(playerFull, new[] { creator }, TestCountryContinents.Map);

        _playerService
            .Setup(_ => _.GetPlayerSubmissionsAsync(TestCountryContinents.Map))
            .ReturnsAsync(new[] { submittedPlayer });

        var result = await _controller.PlayerSubmission();

        var model = ((ViewResult)result).Model.Should().BeOfType<PlayerSubmissionsModel>().Subject;
        var item = model.Players.Should().ContainSingle().Subject;
        item.Name.Should().Be("Zinédine Zidane");
        item.Country.Should().Be("France");
        item.Continent.Should().Be("Europe");
        item.YearOfBirth.Should().Be(1972);
    }

    [Fact]
    public async Task AcceptPlayer_NoSubmissionsLeft_RedirectsToPlayerSubmission()
    {
        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);
        _internationalService.Setup(_ => _.GetCountriesAsync(It.IsAny<Languages>())).ReturnsAsync(new Dictionary<ulong, string>());
        _internationalService.Setup(_ => _.GetContinentsAsync(It.IsAny<Languages>())).ReturnsAsync(new Dictionary<ulong, string>());
        _playerService.Setup(_ => _.GetPlayerSubmissionsAsync(TestCountryContinents.Map)).ReturnsAsync(Array.Empty<Player>());

        var result = await _controller.AcceptPlayer(new PlayerSubmissionsModel { SelectedId = 42 });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "PlayerSubmission" && r.ControllerName == "Admin");
    }

    private void SetupOnePendingSubmission()
    {
        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);
        _internationalService.Setup(_ => _.GetCountriesAsync(It.IsAny<Languages>()))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Countries.FRA, "France" } });
        _internationalService.Setup(_ => _.GetContinentsAsync(It.IsAny<Languages>()))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Continents.Europe, "Europe" } });
        var playerFull = PlayerFullDtoBuilder.Valid().Build();
        var creator = UserDtoBuilder.Valid().WithId(playerFull.Player.CreationUserId).WithLogin("createur").Build();
        var submittedPlayer = new Player(playerFull, new[] { creator }, TestCountryContinents.Map);
        _playerService.Setup(_ => _.GetPlayerSubmissionsAsync(TestCountryContinents.Map)).ReturnsAsync(new[] { submittedPlayer });
    }

    [Fact]
    public async Task AcceptPlayer_WithAFutureDate_ForcesIt()
    {
        SetupOnePendingSubmission();
        _playerService.Setup(_ => _.ValidatePlayerSubmissionAsync(It.IsAny<PlayerSubmissionValidationRequest>()))
            .ReturnsAsync((PlayerSubmissionErrors.NoError, 42UL, (IReadOnlyCollection<Badges>)Array.Empty<Badges>()));

        var model = new PlayerSubmissionsModel
        {
            SelectedId = 1,
            ClueOverwriteFr = "indice",
            EasyClueOverwriteFr = "facile",
            PublicationDate = Today.AddDays(5).ToString("yyyy-MM-dd")
        };

        var result = await _controller.AcceptPlayer(model);

        _playerService.Verify(_ => _.ValidatePlayerSubmissionAsync(
            It.Is<PlayerSubmissionValidationRequest>(r => r.PublicationDate == Today.AddDays(5))), Times.Once);
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AcceptPlayer_WithATodayOrPastDate_IsRejected(int daysFromToday)
    {
        SetupOnePendingSubmission();

        var model = new PlayerSubmissionsModel
        {
            SelectedId = 1,
            ClueOverwriteFr = "indice",
            EasyClueOverwriteFr = "facile",
            PublicationDate = Today.AddDays(daysFromToday).ToString("yyyy-MM-dd")
        };

        var result = await _controller.AcceptPlayer(model);

        var resultModel = ((ViewResult)result).Model.Should().BeOfType<PlayerSubmissionsModel>().Subject;
        resultModel.ErrorMessage.Should().Be("InvalidPublicationDate");
        _playerService.Verify(_ => _.ValidatePlayerSubmissionAsync(It.IsAny<PlayerSubmissionValidationRequest>()), Times.Never);
    }

    [Fact]
    public async Task RefusePlayer_IgnoresAnyPublicationDate()
    {
        // le champ n'a de sens qu'a l'acceptation ; le presence d'une valeur ne doit pas
        // faire echouer un refus
        SetupOnePendingSubmission();
        _playerService.Setup(_ => _.ValidatePlayerSubmissionAsync(It.IsAny<PlayerSubmissionValidationRequest>()))
            .ReturnsAsync((PlayerSubmissionErrors.NoError, 42UL, (IReadOnlyCollection<Badges>)Array.Empty<Badges>()));

        var model = new PlayerSubmissionsModel
        {
            SelectedId = 1,
            RefusalReason = "doublon",
            PublicationDate = Today.ToString("yyyy-MM-dd")
        };

        var result = await _controller.RefusePlayer(model);

        result.Should().BeOfType<RedirectToActionResult>();
        _playerService.Verify(_ => _.ValidatePlayerSubmissionAsync(
            It.Is<PlayerSubmissionValidationRequest>(r => r.PublicationDate == null)), Times.Once);
    }

    // ------------------------------------------------------------- Actions (annonces/outils)

    [Fact]
    public async Task Actions_Get_RendersDefaultMessageDates()
    {
        var tomorrowEnd = Today.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(23);
        _clock.Setup(_ => _.NowSeconds).Returns(Today.ToDateTime(TimeOnly.MinValue));
        _clock.Setup(_ => _.TomorrowEnd).Returns(tomorrowEnd);

        var result = await _controller.Actions();

        var model = ((ViewResult)result).Model.Should().BeOfType<AdminModel>().Subject;
        model.MessageDateStart.Should().Be(Today.ToDateTime(TimeOnly.MinValue));
        model.MessageDateEnd.Should().Be(tomorrowEnd);
    }

    [Fact]
    public async Task RecomputeBadges_Post_ResetsBadges()
    {
        var result = await _controller.RecomputeBadges();

        _badgeService.Verify(_ => _.ResetBadgesAsync(It.IsAny<Languages>(), It.IsAny<IReadOnlyDictionary<ulong, ulong>>()), Times.Once);
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task RecomputeLeaders_Post_ComputesMissingLeadersWithCountryContinents()
    {
        _internationalService.Setup(_ => _.GetCountryContinentsAsync()).ReturnsAsync(TestCountryContinents.Map);

        await _controller.RecomputeLeaders();

        _leaderService.Verify(_ => _.ComputeMissingLeadersAsync(TestCountryContinents.Map), Times.Once);
    }

    [Fact]
    public async Task ReassignPlayers_Post_ReassignsThePlayersOfTheDay()
    {
        await _controller.ReassignPlayers();

        _playerService.Verify(_ => _.ReassignPlayersOfTheDayAsync(), Times.Once);
    }

    [Fact]
    public async Task InsertMessage_Post_InsertsTheMessageAndSetsFeedback()
    {
        var start = Today.ToDateTime(TimeOnly.MinValue);
        var end = Today.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var result = await _controller.InsertMessage(new AdminModel
        {
            Message = "une annonce",
            MessageDateStart = start,
            MessageDateEnd = end
        });

        _messageRepository.Verify(_ => _.InsertMessageAsync(It.Is<KikoleSite.Models.Dtos.MessageDto>(
            m => m.Message == "une annonce" && m.DisplayFrom == start && m.DisplayTo == end)), Times.Once);

        var model = ((ViewResult)result).Model.Should().BeOfType<AdminModel>().Subject;
        model.ActionFeedback.Should().Be("Annonce créée");
    }

    // ------------------------------------------------------------- Discussions / Discussion

    [Fact]
    public async Task Discussions_Get_ListsAllDiscussions()
    {
        var discussions = new[]
        {
            new KikoleSite.Models.Dtos.DiscussionSummaryDto { DiscussionId = 1, UserId = 2, UserLogin = "joueur1" }
        };
        _discussionService.Setup(_ => _.GetAllDiscussionsAsync()).ReturnsAsync(discussions);

        var result = await _controller.Discussions();

        var model = ((ViewResult)result).Model.Should().BeOfType<AdminDiscussionsModel>().Subject;
        model.Discussions.Should().BeSameAs(discussions);
    }

    [Fact]
    public async Task DiscussionGet_UnknownDiscussion_RedirectsToDiscussions()
    {
        _discussionService.Setup(_ => _.GetAllDiscussionsAsync()).ReturnsAsync(Array.Empty<KikoleSite.Models.Dtos.DiscussionSummaryDto>());

        var result = await _controller.Discussion(discussionId: 5);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "Discussions" && r.ControllerName == "Admin");
    }

    [Fact]
    public async Task DiscussionGet_KnownDiscussion_RendersItsThread()
    {
        var discussions = new[]
        {
            new KikoleSite.Models.Dtos.DiscussionSummaryDto { DiscussionId = 5, UserId = 2, UserLogin = "joueur1" }
        };
        _discussionService.Setup(_ => _.GetAllDiscussionsAsync()).ReturnsAsync(discussions);
        var messages = new[]
        {
            new KikoleSite.Models.Dtos.DiscussionMessageDto { Id = 1, DiscussionId = 5, Message = "bonjour" }
        };
        _discussionService.Setup(_ => _.GetThreadForAdminAsync(5)).ReturnsAsync(messages);

        var result = await _controller.Discussion(discussionId: 5);

        var model = ((ViewResult)result).Model.Should().BeOfType<AdminDiscussionModel>().Subject;
        model.UserLogin.Should().Be("joueur1");
        model.Messages.Should().BeSameAs(messages);
    }

    [Fact]
    public async Task DiscussionPost_EmptyMessage_SetsInvalidMessageError()
    {
        _discussionService.Setup(_ => _.GetAllDiscussionsAsync()).ReturnsAsync(Array.Empty<KikoleSite.Models.Dtos.DiscussionSummaryDto>());
        _discussionService.Setup(_ => _.GetThreadForAdminAsync(5)).ReturnsAsync(Array.Empty<KikoleSite.Models.Dtos.DiscussionMessageDto>());

        var result = await _controller.Discussion(new AdminDiscussionModel { DiscussionId = 5, UserLogin = "x", NewMessage = "  " });

        var model = ((ViewResult)result).Model.Should().BeOfType<AdminDiscussionModel>().Subject;
        model.ErrorMessage.Should().Be("InvalidMessage");
        _discussionService.Verify(_ => _.PostAdminReplyAsync(It.IsAny<ulong>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DiscussionPost_ValidMessage_PostsTheReplyAndRefreshesTheThread()
    {
        var discussions = new[]
        {
            new KikoleSite.Models.Dtos.DiscussionSummaryDto { DiscussionId = 5, UserId = 2, UserLogin = "joueur1" }
        };
        _discussionService.Setup(_ => _.GetAllDiscussionsAsync()).ReturnsAsync(discussions);
        var refreshedMessages = new[]
        {
            new KikoleSite.Models.Dtos.DiscussionMessageDto { Id = 1, DiscussionId = 5, Message = "bonjour", IsFromAdmin = true }
        };
        _discussionService.Setup(_ => _.GetThreadForAdminAsync(5)).ReturnsAsync(refreshedMessages);

        var result = await _controller.Discussion(new AdminDiscussionModel { DiscussionId = 5, UserLogin = "x", NewMessage = "une reponse" });

        _discussionService.Verify(_ => _.PostAdminReplyAsync(5, "une reponse"), Times.Once);
        var model = ((ViewResult)result).Model.Should().BeOfType<AdminDiscussionModel>().Subject;
        model.UserLogin.Should().Be("joueur1");
        model.Messages.Should().BeSameAs(refreshedMessages);
        model.NewMessage.Should().BeNull();
    }

    // ------------------------------------------------------------- Index (creation de joueur)

    [Fact]
    public async Task PlayerCreationPost_MissingName_SetsMandatNameError()
    {
        var result = await _controller.Index(new PlayerCreationModel());

        var model = ((ViewResult)result).Model.Should().BeOfType<PlayerCreationModel>().Subject;
        model.ErrorMessage.Should().Be("MandatName");
        _playerService.Verify(_ => _.CreatePlayerAsync(It.IsAny<KikoleSite.Models.Requests.PlayerRequest>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task PlayerCreationPost_ValidMinimalSubmission_CreatesThePlayer()
    {
        _internationalService.Setup(_ => _.GetCountriesAsync(It.IsAny<Languages>()))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Countries.FRA, "France" } });
        var club = new Club(ClubDtoBuilder.Valid().WithId(7).Build(), []);
        _internationalService.Setup(_ => _.GetClubsAsync()).ReturnsAsync(new[] { club });

        var model = new PlayerCreationModel
        {
            Name = "Zinédine Zidane",
            AlternativeName0 = "zizou",
            YearOfBirth = "1972",
            ClueEn = "clue",
            EasyClueEn = "easy clue",
            Country = ((ulong)Countries.FRA).ToString(),
            Position = ((ulong)Positions.Midfielder).ToString(),
            Club0Id = "7"
        };

        var result = await _controller.Index(model);

        _playerService.Verify(_ => _.CreatePlayerAsync(
            It.Is<KikoleSite.Models.Requests.PlayerRequest>(r => r.Name == "Zinédine Zidane" && r.Clubs.Count == 1),
            1), Times.Once);
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "Index" && r.ControllerName == "Admin");
    }

    private static PlayerCreationModel MinimalPlayerCreationModel(string? publicationDate = null) => new()
    {
        Name = "Zinédine Zidane",
        AlternativeName0 = "zizou",
        YearOfBirth = "1972",
        ClueEn = "clue",
        EasyClueEn = "easy clue",
        Country = ((ulong)Countries.FRA).ToString(),
        Position = ((ulong)Positions.Midfielder).ToString(),
        Club0Id = "7",
        PublicationDate = publicationDate
    };

    [Fact]
    public async Task PlayerCreationPost_AsAdministratorWithAFutureDate_ForcesIt()
    {
        _internationalService.Setup(_ => _.GetCountriesAsync(It.IsAny<Languages>()))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Countries.FRA, "France" } });
        _internationalService.Setup(_ => _.GetClubsAsync())
            .ReturnsAsync(new[] { new Club(ClubDtoBuilder.Valid().WithId(7).Build(), []) });

        var model = MinimalPlayerCreationModel(Today.AddDays(5).ToString("yyyy-MM-dd"));

        var result = await _controller.Index(model);

        _playerService.Verify(_ => _.CreatePlayerAsync(
            It.Is<KikoleSite.Models.Requests.PlayerRequest>(r => r.PublicationDate == Today.AddDays(5)),
            1), Times.Once);
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PlayerCreationPost_AsAdministratorWithATodayOrPastDate_IsRejected(int daysFromToday)
    {
        _internationalService.Setup(_ => _.GetCountriesAsync(It.IsAny<Languages>()))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Countries.FRA, "France" } });
        _internationalService.Setup(_ => _.GetClubsAsync())
            .ReturnsAsync(new[] { new Club(ClubDtoBuilder.Valid().WithId(7).Build(), []) });

        var model = MinimalPlayerCreationModel(Today.AddDays(daysFromToday).ToString("yyyy-MM-dd"));

        var result = await _controller.Index(model);

        var resultModel = ((ViewResult)result).Model.Should().BeOfType<PlayerCreationModel>().Subject;
        resultModel.ErrorMessage.Should().Be("InvalidPublicationDate");
        _playerService.Verify(_ => _.CreatePlayerAsync(It.IsAny<KikoleSite.Models.Requests.PlayerRequest>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task PlayerCreationPost_AsPowerUserWithADateInThePayload_TheDateIsIgnored()
    {
        // le champ n'existe pas dans le formulaire PowerUser, mais on se protege aussi
        // cote serveur d'une requete bricolee
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)UserTypes.PowerUser).ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null));
        _internationalService.Setup(_ => _.GetCountriesAsync(It.IsAny<Languages>()))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Countries.FRA, "France" } });
        _internationalService.Setup(_ => _.GetClubsAsync())
            .ReturnsAsync(new[] { new Club(ClubDtoBuilder.Valid().WithId(7).Build(), []) });

        var model = MinimalPlayerCreationModel(Today.AddDays(5).ToString("yyyy-MM-dd"));

        await _controller.Index(model);

        _playerService.Verify(_ => _.CreatePlayerAsync(
            It.Is<KikoleSite.Models.Requests.PlayerRequest>(r => r.PublicationDate == null),
            1), Times.Once);
    }

    // ------------------------------------------------------------- UploadClueMedia

    private static IFormFile BuildFormFile(string fileName, long length, byte[]? content = null)
    {
        content ??= new byte[Math.Min(length, 16)];
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, length, "file", fileName);
    }

    [Theory]
    [InlineData("indice.png")]
    [InlineData("indice.MP3")]
    [InlineData("indice.mp4")]
    public async Task UploadClueMedia_AllowedExtension_SavesTheFileAndReturnsItsRootRelativePath(string fileName)
    {
        var content = new byte[] { 1, 2, 3, 4 };
        var file = BuildFormFile(fileName, content.Length, content);

        var result = await _controller.UploadClueMedia(file);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        var path = json.Value!.GetType().GetProperty("path")!.GetValue(json.Value) as string;
        path.Should().NotBeNullOrWhiteSpace();
        path.Should().MatchRegex(@"^/media/clues/[0-9a-f-]+\." + fileName.Split('.')[1].ToLowerInvariant() + "$");

        var savedFilePath = Path.Combine(_webRootPath, path!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        File.Exists(savedFilePath).Should().BeTrue();
        File.ReadAllBytes(savedFilePath).Should().BeEquivalentTo(content);
    }

    [Theory]
    [InlineData("indice.exe")]
    [InlineData("indice.txt")]
    [InlineData("indice")]
    public async Task UploadClueMedia_DisallowedExtension_ReturnsBadRequest(string fileName)
    {
        var file = BuildFormFile(fileName, 4);

        var result = await _controller.UploadClueMedia(file);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task UploadClueMedia_NoFile_ReturnsBadRequest()
    {
        var result = await _controller.UploadClueMedia(null);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task UploadClueMedia_EmptyFile_ReturnsBadRequest()
    {
        var file = BuildFormFile("indice.png", 0);

        var result = await _controller.UploadClueMedia(file);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task UploadClueMedia_FileTooLarge_ReturnsBadRequest()
    {
        // Length declare volontairement au-dela du plafond (15 Mo) sans allouer un
        // vrai flux de cette taille : le controleur rejette avant toute lecture du flux
        var file = BuildFormFile("indice.mp4", (15 * 1024 * 1024) + 1);

        var result = await _controller.UploadClueMedia(file);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Theory]
    [InlineData("indice.mp3")]
    [InlineData("indice.MP4")]
    public async Task UploadClueMedia_AudioOrVideoAsPowerUser_ReturnsForbidden(string fileName)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)UserTypes.PowerUser).ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null));
        var file = BuildFormFile(fileName, 4);

        var result = await _controller.UploadClueMedia(file);

        // pas Forbid() : sous l'authentification par cookie, ce ForbidResult serait
        // intercepte et transforme en redirection (302) plutot qu'un vrai 403 - verifie
        // en direct, cf. commentaire dans AdminController.UploadClueMedia
        result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(403);
        File.Exists(Path.Combine(_webRootPath, "media", "clues")).Should().BeFalse();
    }

    [Fact]
    public async Task UploadClueMedia_ImageAsPowerUser_StillAllowed()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)UserTypes.PowerUser).ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null));
        var content = new byte[] { 1, 2, 3 };
        var file = BuildFormFile("indice.png", content.Length, content);

        var result = await _controller.UploadClueMedia(file);

        result.Should().BeOfType<JsonResult>();
    }

    // ------------------------------------------------------------- Club

    [Fact]
    public async Task ClubGet_NoId_RendersAnEmptyModel()
    {
        var result = await _controller.Club(clubId: 0);

        var model = ((ViewResult)result).Model.Should().BeOfType<ClubCreationModel>().Subject;
        model.Id.Should().Be(0);
    }

    [Fact]
    public async Task ClubGet_AsAdministrator_RendersTheExistingClub()
    {
        var club = new Club(
            ClubDtoBuilder.Valid().WithId(7).WithCountryId((ulong)Countries.ESP).Build(),
            [
                ClubTranslationDtoBuilder.Valid().WithClubId(7).WithLanguage(Languages.en).WithName("Real Madrid").Build(),
                ClubTranslationDtoBuilder.Valid().WithClubId(7).WithLanguage(Languages.fr).WithName("Real Madrid").Build()
            ]);
        _internationalService.Setup(_ => _.GetClubAsync(7)).ReturnsAsync(club);

        var result = await _controller.Club(clubId: 7);

        var model = ((ViewResult)result).Model.Should().BeOfType<ClubCreationModel>().Subject;
        model.MainNameEn.Should().Be("Real Madrid");
        model.Id.Should().Be(7);
    }

    [Fact]
    public async Task ClubGet_AsNonAdministrator_RedirectsToErrorIndex()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)UserTypes.PowerUser).ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null));

        var result = await _controller.Club(clubId: 7);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "ErrorIndex" && r.ControllerName == "Home");
    }

    [Fact]
    public async Task ClubPost_MissingNames_SetsClubNameMissError()
    {
        var result = await _controller.Club(new ClubCreationModel());

        var model = ((ViewResult)result).Model.Should().BeOfType<ClubCreationModel>().Subject;
        model.ErrorMessage.Should().Be("ClubNameMiss");
        _internationalService.Verify(_ => _.CreateOrUpdateClubAsync(It.IsAny<KikoleSite.Models.Requests.ClubRequest>()), Times.Never);
    }

    [Fact]
    public async Task ClubPost_ValidSubmission_CreatesOrUpdatesTheClub()
    {
        _internationalService.Setup(_ => _.GetCountriesAsync(It.IsAny<Languages>()))
            .ReturnsAsync(new Dictionary<ulong, string> { { (ulong)Countries.ESP, "Espagne" } });

        var result = await _controller.Club(new ClubCreationModel
        {
            MainNameEn = "Real Madrid",
            MainNameFr = "Real Madrid",
            Country = ((ulong)Countries.ESP).ToString()
        });

        _internationalService.Verify(_ => _.CreateOrUpdateClubAsync(It.IsAny<KikoleSite.Models.Requests.ClubRequest>()), Times.Once);
        var model = ((ViewResult)result).Model.Should().BeOfType<ClubCreationModel>().Subject;
        model.InfoMessage.Should().Be("ClubOk");
    }

    // ------------------------------------------------------------- PlayerEdit

    [Fact]
    public async Task PlayerEditGet_PopulatesCluesFromBothLanguages()
    {
        _playerService
            .Setup(_ => _.GetPlayerCluesAsync(7, It.IsAny<IReadOnlyCollection<Languages>>()))
            .ReturnsAsync(new Dictionary<Languages, (string?, string?)>
            {
                { Languages.en, ("clue en", "easy en") },
                { Languages.fr, ("clue fr", "easy fr") }
            });

        var result = await _controller.PlayerEdit(playerId: 7);

        var model = ((ViewResult)result).Model.Should().BeOfType<PlayerEditModel>().Subject;
        model.ClueEn.Should().Be("clue en");
        model.ClueFr.Should().Be("clue fr");
    }

    [Fact]
    public async Task PlayerEditPost_MissingClue_DoesNotUpdate()
    {
        var result = await _controller.PlayerEdit(new PlayerEditModel { PlayerId = 7 });

        var model = ((ViewResult)result).Model.Should().BeOfType<PlayerEditModel>().Subject;
        model.Success.Should().NotBe(true);
        _playerService.Verify(_ => _.UpdatePlayerCluesAsync(
            It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<Languages, string?>>(), It.IsAny<IReadOnlyDictionary<Languages, string?>>()),
            Times.Never);
    }

    [Fact]
    public async Task PlayerEditPost_ValidSubmission_UpdatesTheClues()
    {
        var result = await _controller.PlayerEdit(new PlayerEditModel
        {
            PlayerId = 7,
            ClueEn = "clue en",
            ClueFr = "clue fr",
            EasyClueEn = "easy en",
            EasyClueFr = "easy fr"
        });

        _playerService.Verify(_ => _.UpdatePlayerCluesAsync(7, "clue en", "easy en",
            It.Is<IReadOnlyDictionary<Languages, string?>>(d => d[Languages.fr] == "clue fr"),
            It.Is<IReadOnlyDictionary<Languages, string?>>(d => d[Languages.fr] == "easy fr")),
            Times.Once);

        var model = ((ViewResult)result).Model.Should().BeOfType<PlayerEditModel>().Subject;
        model.Success.Should().BeTrue();
    }

    // ------------------------------------------------------------- gestion des utilisateurs

    private static UsersModel UsersViewModel(IActionResult result)
    {
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.ViewName.Should().Be("Users");
        return view.Model.Should().BeOfType<UsersModel>().Subject;
    }

    private void SetupUser(ulong id, UserTypes type = UserTypes.StandardUser, string login = "joueur")
    {
        _userRepository.Setup(_ => _.GetUserByIdAsync(id))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(id).WithLogin(login).WithType(type).Build());
    }

    [Fact]
    public async Task Users_ListsRowsWithDecryptedEmails()
    {
        var users = new List<KikoleSite.Models.Dtos.UserDto>
        {
            UserDtoBuilder.Valid().WithId(5).WithLogin("lea").WithEmailEncrypted("chiffre").Build()
        };
        _userRepository
            .Setup(_ => _.SearchUsersAsync("le", UserStatusFilter.Enabled, null, true, 1, UsersModel.PageSize))
            .ReturnsAsync(((IReadOnlyList<KikoleSite.Models.Dtos.UserDto>)users, 1));

        var result = await _controller.Users(new UserListQuery { Login = "le", Status = UserStatusFilter.Enabled, Desc = true });

        var model = UsersViewModel(result);
        model.TotalCount.Should().Be(1);
        model.Rows.Should().ContainSingle().Which.Email.Should().Be("plain-chiffre");
    }

    [Fact]
    public async Task Users_FilteringOnAdministratorsIsIgnored()
    {
        _userRepository
            .Setup(_ => _.SearchUsersAsync(null, UserStatusFilter.All, null, false, 1, UsersModel.PageSize))
            .ReturnsAsync(((IReadOnlyList<KikoleSite.Models.Dtos.UserDto>)new List<KikoleSite.Models.Dtos.UserDto>(), 0));

        var result = await _controller.Users(new UserListQuery { Type = UserTypes.Administrator });

        UsersViewModel(result).Query.Type.Should().BeNull();
    }

    [Fact]
    public async Task AutoCompleteUserLogins_WithABlankPrefix_ReturnsNothingWithoutQuerying()
    {
        var result = await _controller.AutoCompleteUserLogins("  ");

        ((string[])result.Value!).Should().BeEmpty();
        _userRepository.Verify(_ => _.SearchLoginsAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DisableUser_WithoutReason_DisablesNothing()
    {
        SetupUser(5);

        var result = await _controller.DisableUser(new UserActionRequest { UserId = 5, Reason = "  " });

        UsersViewModel(result).Error.Should().Be("DisableReasonInvalid");
        _userRepository.Verify(_ => _.DisableUserAsync(It.IsAny<ulong>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DisableUser_OnAnAdministrator_IsRefused()
    {
        SetupUser(1, UserTypes.Administrator, "admin");

        var result = await _controller.DisableUser(new UserActionRequest { UserId = 1, Reason = "test" });

        UsersViewModel(result).Error.Should().Be("UserNotManageable");
        _userRepository.Verify(_ => _.DisableUserAsync(It.IsAny<ulong>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DisableUser_OnAnAlreadyDisabledAccount_IsRefused()
    {
        _userRepository.Setup(_ => _.GetUserByIdAsync(5)).ReturnsAsync((KikoleSite.Models.Dtos.UserDto?)null);

        var result = await _controller.DisableUser(new UserActionRequest { UserId = 5, Reason = "test" });

        UsersViewModel(result).Error.Should().Be("UserNotManageable");
        _userRepository.Verify(_ => _.DisableUserAsync(It.IsAny<ulong>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DisableUser_Valid_DisablesWithTheTrimmedReason()
    {
        SetupUser(5, login: "lea");

        var result = await _controller.DisableUser(new UserActionRequest { UserId = 5, Reason = "  multi-compte  " });

        _userRepository.Verify(_ => _.DisableUserAsync(5, "multi-compte"), Times.Once);
        UsersViewModel(result).Feedback.Should().Be("UserDisabled:lea");
    }

    [Fact]
    public async Task ForceUserPassword_OnAnAdministrator_IsRefused()
    {
        SetupUser(1, UserTypes.Administrator, "admin");

        var result = await _controller.ForceUserPassword(new UserActionRequest { UserId = 1, NewPassword = "un-mot-de-passe-long", NewPasswordConfirm = "un-mot-de-passe-long" });

        UsersViewModel(result).Error.Should().Be("UserNotManageable");
        _userManager.Verify(_ => _.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForceUserPassword_WhenTheConfirmationDiffers_ChangesNothing()
    {
        SetupUser(5);

        var result = await _controller.ForceUserPassword(
            new UserActionRequest { UserId = 5, NewPassword = "un-mot-de-passe-long", NewPasswordConfirm = "autre-mot-de-passe" });

        UsersViewModel(result).Error.Should().Be("PasswordMismatch");
        _userManager.Verify(_ => _.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForceUserPassword_Valid_ReplacesThePassword()
    {
        SetupUser(5, login: "lea");
        var user = new ApplicationUser { Id = 5, UserName = "lea" };
        _userManager.Setup(_ => _.FindByIdAsync("5")).ReturnsAsync(user);
        _userManager.Setup(_ => _.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("jeton");
        _userManager.Setup(_ => _.ResetPasswordAsync(user, "jeton", "un-mot-de-passe-long")).ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ForceUserPassword(new UserActionRequest { UserId = 5, NewPassword = "un-mot-de-passe-long", NewPasswordConfirm = "un-mot-de-passe-long" });

        UsersViewModel(result).Feedback.Should().Be("PasswordForced:lea");
        _userManager.Verify(_ => _.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task ForceUserPassword_WhenTheValidatorRejectsIt_ReportsTheReason()
    {
        SetupUser(5);
        var user = new ApplicationUser { Id = 5, UserName = "lea" };
        _userManager.Setup(_ => _.FindByIdAsync("5")).ReturnsAsync(user);
        _userManager.Setup(_ => _.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("jeton");
        _userManager.Setup(_ => _.ResetPasswordAsync(user, "jeton", "court"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = nameof(IdentityErrorDescriber.PasswordTooShort) }));

        var result = await _controller.ForceUserPassword(new UserActionRequest { UserId = 5, NewPassword = "court", NewPasswordConfirm = "court" });

        UsersViewModel(result).Error.Should().Be("PasswordTooShort");
    }

    [Theory]
    [InlineData(UserTypes.Administrator)]
    [InlineData(null)]
    public async Task ChangeUserType_ToAnythingButStandardOrPower_IsRefused(UserTypes? newType)
    {
        SetupUser(5);

        var result = await _controller.ChangeUserType(new UserActionRequest { UserId = 5, NewType = newType });

        UsersViewModel(result).Error.Should().Be("UserNotManageable");
        _userRepository.Verify(_ => _.ChangeUserTypeAsync(It.IsAny<ulong>(), It.IsAny<UserTypes>()), Times.Never);
    }

    [Fact]
    public async Task ChangeUserType_ToTheCurrentType_IsRefused()
    {
        SetupUser(5, UserTypes.PowerUser);

        var result = await _controller.ChangeUserType(new UserActionRequest { UserId = 5, NewType = UserTypes.PowerUser });

        UsersViewModel(result).Error.Should().Be("UserNotManageable");
        _userRepository.Verify(_ => _.ChangeUserTypeAsync(It.IsAny<ulong>(), It.IsAny<UserTypes>()), Times.Never);
    }

    [Fact]
    public async Task ChangeUserType_Valid_ChangesTheType()
    {
        SetupUser(5, login: "lea");

        var result = await _controller.ChangeUserType(new UserActionRequest { UserId = 5, NewType = UserTypes.PowerUser });

        _userRepository.Verify(_ => _.ChangeUserTypeAsync(5, UserTypes.PowerUser), Times.Once);
        UsersViewModel(result).Feedback.Should().Be("UserTypeChanged:lea");
    }

    [Fact]
    public async Task DeleteUser_WithAnotherLogin_DeletesNothing()
    {
        _userRepository.Setup(_ => _.GetUserByIdIncludingDisabledAsync(5))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(5).WithLogin("lea").Build());

        var result = await _controller.DeleteUser(new UserActionRequest { UserId = 5, LoginConfirmation = "hugo" });

        UsersViewModel(result).Error.Should().Be("LoginConfirmationMismatch");
        _userRepository.Verify(_ => _.DeleteUserWithAllDataAsync(It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUser_OnAnAdministrator_IsRefused()
    {
        _userRepository.Setup(_ => _.GetUserByIdIncludingDisabledAsync(1))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(1).WithLogin("admin").WithType(UserTypes.Administrator).Build());

        var result = await _controller.DeleteUser(new UserActionRequest { UserId = 1, LoginConfirmation = "admin" });

        UsersViewModel(result).Error.Should().Be("UserNotDeletable");
        _userRepository.Verify(_ => _.DeleteUserWithAllDataAsync(It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUser_OnAnUnknownAccount_IsRefused()
    {
        _userRepository.Setup(_ => _.GetUserByIdIncludingDisabledAsync(9)).ReturnsAsync((KikoleSite.Models.Dtos.UserDto?)null);

        var result = await _controller.DeleteUser(new UserActionRequest { UserId = 9, LoginConfirmation = "x" });

        UsersViewModel(result).Error.Should().Be("UserNotDeletable");
        _userRepository.Verify(_ => _.DeleteUserWithAllDataAsync(It.IsAny<ulong>()), Times.Never);
    }

    [Theory]
    [InlineData("lea")]
    [InlineData("  LEA ")]
    public async Task DeleteUser_WithTheRetypedLogin_DeletesTheAccount(string confirmation)
    {
        _userRepository.Setup(_ => _.GetUserByIdIncludingDisabledAsync(5))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(5).WithLogin("lea").WithDisabled().Build());
        _userRepository.Setup(_ => _.DeleteUserWithAllDataAsync(5)).ReturnsAsync(true);

        var result = await _controller.DeleteUser(new UserActionRequest { UserId = 5, LoginConfirmation = confirmation });

        _userRepository.Verify(_ => _.DeleteUserWithAllDataAsync(5), Times.Once);
        UsersViewModel(result).Feedback.Should().Be("UserDeleted:lea");
    }

    [Fact]
    public async Task DeleteUser_WhenTheRepositoryRefuses_ReportsIt()
    {
        _userRepository.Setup(_ => _.GetUserByIdIncludingDisabledAsync(5))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(5).WithLogin("lea").Build());
        _userRepository.Setup(_ => _.DeleteUserWithAllDataAsync(5)).ReturnsAsync(false);

        var result = await _controller.DeleteUser(new UserActionRequest { UserId = 5, LoginConfirmation = "lea" });

        UsersViewModel(result).Error.Should().Be("UserNotDeletable");
    }
}
