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
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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
    private static readonly DateTime Today = TestCalendar.FirstDate.AddDays(30);

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
    private readonly string _webRootPath = Path.Combine(Path.GetTempPath(), "kikole-tests-" + Guid.NewGuid());
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _clock.Setup(_ => _.Today).Returns(Today);
        _clock.Setup(_ => _.Now).Returns(Today);
        _localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));
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
            _webHostEnvironment.Object)
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

    // ------------------------------------------------------------- Actions (annonces/outils)

    [Fact]
    public async Task Actions_Get_RendersDefaultMessageDates()
    {
        var tomorrowEnd = Today.AddDays(1).AddHours(23);
        _clock.Setup(_ => _.NowSeconds).Returns(Today);
        _clock.Setup(_ => _.TomorrowEnd).Returns(tomorrowEnd);

        var result = await _controller.Actions();

        var model = ((ViewResult)result).Model.Should().BeOfType<AdminModel>().Subject;
        model.MessageDateStart.Should().Be(Today);
        model.MessageDateEnd.Should().Be(tomorrowEnd);
    }

    [Fact]
    public async Task RecomputeBadges_Post_ResetsBadges()
    {
        var result = await _controller.RecomputeBadges();

        _badgeService.Verify(_ => _.ResetBadgesAsync(It.IsAny<Languages>()), Times.Once);
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
        var start = Today;
        var end = Today.AddDays(1);

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
}
