using System;
using System.Collections.Generic;
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
public class AdminControllerTests
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
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _clock.Setup(_ => _.Today).Returns(Today);
        _clock.Setup(_ => _.Now).Returns(Today);
        _localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

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
            httpContextAccessor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(UserTypeClaimsPrincipalFactory.UserTypeClaimType, ((ulong)UserTypes.Administrator).ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, null));
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
}
