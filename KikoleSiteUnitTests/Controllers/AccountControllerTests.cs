using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using KikoleSite;
using KikoleSite.Configuration;
using KikoleSite.Controllers;
using KikoleSite.Identity;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using KikoleSite.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace KikoleSiteUnitTests.Controllers;

/// <summary>
/// Couvre <see cref="AccountController"/>, jusqu'ici le seul controleur reste hors
/// perimetre apres la levee du blocage Identity (cf. TODO) : contrairement a
/// <see cref="HomeController"/>, Identity y est appele pour de vrai sur presque chaque
/// action (<see cref="UserManager{TUser}"/>/<see cref="SignInManager{TUser}"/> sont
/// mockables comme n'importe quelle dependance, cf. <see cref="IdentityMocks"/> - leurs
/// membres sont <c>virtual</c>). Un scenario heureux et un ou deux echecs par action,
/// pas une couverture exhaustive de chaque branche de validation.
/// </summary>
public class AccountControllerTests
{
    private readonly DefaultHttpContext _httpContext = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IInternationalService> _internationalService = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IGameCalendar> _gameCalendar = TestCalendar.Mock();
    private readonly Mock<IPlayerService> _playerService = new();
    private readonly Mock<IBadgeService> _badgeService = new();
    private readonly Mock<IStringLocalizer<AccountController>> _localizer = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager = IdentityMocks.MockUserManager();
    private readonly Mock<SignInManager<ApplicationUser>> _signInManager;
    private readonly Mock<IEmailProtector> _emailProtector = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IRazorViewRenderer> _emailRenderer = new();
    private readonly Mock<ILogger<AccountController>> _logger = new();
    private readonly Mock<IErrorJournal> _errorJournal = new();
    private readonly Microsoft.Extensions.Caching.Memory.MemoryCache _memoryCache = new(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
    private readonly RegistrationOptions _registrationOptions = new() { SponsorshipEnabled = true };
    // SendingEnabled a false par defaut (comme en local) : la plupart des tests n'ont donc
    // pas a mocker l'envoi, seuls ceux qui portent explicitement sur l'envoi reel le
    // reactivent via BuildController.
    private readonly EmailOptions _emailOptions = new() { FromAddress = "no-reply@kikole.test", SendingEnabled = false };
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _signInManager = IdentityMocks.MockSignInManager(_userManager);

        _localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

        // empreinte deterministe et lisible dans les tests, sans dependre du vrai
        // algorithme de hachage (hors sujet ici, couvert par EmailProtectorTests)
        _emailProtector.Setup(_ => _.Hash(It.IsAny<string>())).Returns<string>(e => $"hash:{e.ToLowerInvariant()}");

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // RenderIndexAsync l'appelle systematiquement des qu'un utilisateur est connecte
        // (section parrainage) : par defaut, aucun filleul, comme pour les autres mocks
        // "silencieux" ci-dessus
        _userRepository.Setup(_ => _.GetGodchildrenAsync(It.IsAny<ulong>())).ReturnsAsync(new List<UserDto>());

        _controller = BuildController(_registrationOptions, _emailOptions);
    }

    /// <summary>Instance dediee pour les tests qui ont besoin d'une config differente de
    /// celle par defaut (<see cref="_registrationOptions"/>/<see cref="_emailOptions"/> sont
    /// immuables une fois passees a <see cref="_controller"/>, cf. <c>init</c>).</summary>
    private AccountController BuildController(RegistrationOptions options)
        => BuildController(options, _emailOptions);

    private AccountController BuildController(RegistrationOptions registrationOptions, EmailOptions emailOptions)
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);

        return new AccountController(
            _localizer.Object,
            _userManager.Object,
            _signInManager.Object,
            new SanitizingLookupNormalizer(),
            _emailProtector.Object,
            _emailSender.Object,
            _emailRenderer.Object,
            _logger.Object,
            new OptionsWrapper<RegistrationOptions>(registrationOptions),
            new OptionsWrapper<EmailOptions>(emailOptions),
            _userRepository.Object,
            _internationalService.Object,
            _clock.Object,
            _gameCalendar.Object,
            _playerService.Object,
            _badgeService.Object,
            httpContextAccessor.Object,
            _memoryCache,
            _errorJournal.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext },
            Url = FakeUrlHelper()
        };
    }

    /// <summary>
    /// Les controleurs instancies a la main (hors pipeline MVC) n'ont pas de <c>Url</c>
    /// fonctionnel par defaut (HttpContext.RequestServices est vide dans ces tests) : les
    /// actions qui construisent un lien (Url.Action, pour les emails) en ont besoin.
    /// </summary>
    private static IUrlHelper FakeUrlHelper()
    {
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("https://kikole.test/fake-link");
        return urlHelper.Object;
    }

    private static ApplicationUser BuildUser(ulong id = 1, string login = "joueur1")
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = login,
            Email = $"{login}@kikole.test",
            EmailConfirmed = true
        };
    }

    // ------------------------------------------------------------- Index / LogOut

    [Fact]
    public async Task Index_Get_RendersTheUnauthenticatedView()
    {
        var result = await _controller.Index();

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task LogOut_SignsOutAndRendersTheDisconnectedView()
    {
        var result = await _controller.LogOut();

        _signInManager.Verify(_ => _.SignOutAsync(), Times.Once);
        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.IsAuthenticated.Should().BeFalse();
    }

    // ------------------------------------------------------------- LogIn

    [Fact]
    public async Task LogIn_EmptyForm_SetsInvalidFormError()
    {
        var result = await _controller.LogIn(new AccountModel());

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("InvalidForm");
        _userManager.Verify(_ => _.FindByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LogIn_UnknownUser_SetsInvalidCredentialsError()
    {
        _userManager.Setup(_ => _.FindByNameAsync("joueur1")).ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.LogIn(new AccountModel { LoginSubmission = "joueur1", PasswordSubmission = "x" });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("InvalidCredentials");
    }

    [Fact]
    public async Task LogIn_Success_RecordsLoginHistoryAndRedirectsToHome()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByNameAsync("joueur1")).ReturnsAsync(user);
        _signInManager
            .Setup(_ => _.PasswordSignInAsync(user, "bonmdp", true, true))
            .ReturnsAsync(SignInResult.Success);

        var result = await _controller.LogIn(new AccountModel { LoginSubmission = "joueur1", PasswordSubmission = "bonmdp" });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "Index" && r.ControllerName == "Home");
        _userRepository.Verify(_ => _.CreateLoginHistoryAsync(1, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task LogIn_LockedOut_SetsAccountLockedOutError()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByNameAsync("joueur1")).ReturnsAsync(user);
        _signInManager
            .Setup(_ => _.PasswordSignInAsync(user, "x", true, true))
            .ReturnsAsync(SignInResult.LockedOut);

        var result = await _controller.LogIn(new AccountModel { LoginSubmission = "joueur1", PasswordSubmission = "x" });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("AccountLockedOut");
    }

    [Fact]
    public async Task LogIn_EmailNotConfirmed_SetsEmailNotConfirmedError()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByNameAsync("joueur1")).ReturnsAsync(user);
        _signInManager
            .Setup(_ => _.PasswordSignInAsync(user, "x", true, true))
            .ReturnsAsync(SignInResult.NotAllowed);

        var result = await _controller.LogIn(new AccountModel { LoginSubmission = "joueur1", PasswordSubmission = "x" });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("EmailNotConfirmed");
    }

    [Fact]
    public async Task LogIn_ByEmail_WhenLoginDoesNotMatchButEmailDoes_Succeeds()
    {
        // le champ de connexion accepte le login OU l'email : le login echoue d'abord,
        // puis l'email est essaye
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByNameAsync("joueur1@kikole.test")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(_ => _.FindByEmailAsync("joueur1@kikole.test")).ReturnsAsync(user);
        _signInManager
            .Setup(_ => _.PasswordSignInAsync(user, "bonmdp", true, true))
            .ReturnsAsync(SignInResult.Success);

        var result = await _controller.LogIn(new AccountModel { LoginSubmission = "joueur1@kikole.test", PasswordSubmission = "bonmdp" });

        result.Should().BeOfType<RedirectToActionResult>();
    }

    // ------------------------------------------------------------- ConfirmEmail

    [Fact]
    public async Task ConfirmEmail_MissingToken_SetsError()
    {
        var result = await _controller.ConfirmEmail("1", "");

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("EmailConfirmationFailed");
    }

    [Fact]
    public async Task ConfirmEmail_Success_SetsSuccessInfo()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.ConfirmEmailAsync(user, "token")).ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ConfirmEmail("1", "token");

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.SuccessInfo.Should().Be("EmailConfirmed");
        model.Error.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmEmail_InvalidToken_SetsError()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.ConfirmEmailAsync(user, "mauvais")).ReturnsAsync(IdentityResult.Failed());

        var result = await _controller.ConfirmEmail("1", "mauvais");

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("EmailConfirmationFailed");
    }

    // ------------------------------------------------------------- RequestPasswordReset

    [Fact]
    public async Task RequestPasswordReset_UnknownEmail_StillReturnsTheGenericMessage()
    {
        _userManager.Setup(_ => _.FindByEmailAsync("fantome@kikole.test")).ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.RequestPasswordReset(new AccountModel { PasswordResetEmailSubmission = "fantome@kikole.test" });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.SuccessInfo.Should().Be("PasswordResetRequested");
        model.Error.Should().BeNull();
        _userManager.Verify(_ => _.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordReset_KnownEmail_GeneratesATokenAndReturnsTheSameGenericMessage()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByEmailAsync("joueur1@kikole.test")).ReturnsAsync(user);
        _userManager.Setup(_ => _.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");

        var result = await _controller.RequestPasswordReset(new AccountModel { PasswordResetEmailSubmission = "joueur1@kikole.test" });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.SuccessInfo.Should().Be("PasswordResetRequested");
        _userManager.Verify(_ => _.GeneratePasswordResetTokenAsync(user), Times.Once);
        // SendingEnabled=false par defaut dans ces tests : le lien est journalise, pas envoye
        _emailSender.Verify(_ => _.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordReset_AdministratorAccount_DoesNotGenerateAToken()
    {
        // meme message que pour un compte inconnu ou standard : ne doit jamais permettre
        // de reperer un administrateur par ce biais
        var admin = BuildUser();
        admin.UserType = UserTypes.Administrator;
        _userManager.Setup(_ => _.FindByEmailAsync("joueur1@kikole.test")).ReturnsAsync(admin);

        var result = await _controller.RequestPasswordReset(new AccountModel { PasswordResetEmailSubmission = "joueur1@kikole.test" });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.SuccessInfo.Should().Be("PasswordResetRequested");
        _userManager.Verify(_ => _.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    // ------------------------------------------------------------- ResetPassword

    [Fact]
    public void ResetPasswordGet_MissingToken_RedirectsToError()
    {
        var result = _controller.ResetPassword(5, "");

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "ErrorIndex" && r.ControllerName == "Home");
    }

    [Fact]
    public async Task ResetPasswordPost_MismatchedPasswords_SetsInvalidFormError()
    {
        var result = await _controller.ResetPassword(new AccountModel
        {
            ResetPasswordUserId = "1",
            ResetPasswordToken = "token",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "AutreChose1234"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("InvalidForm");
    }

    [Fact]
    public async Task ResetPasswordPost_UnknownUser_SetsResetPasswordError()
    {
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.ResetPassword(new AccountModel
        {
            ResetPasswordUserId = "1",
            ResetPasswordToken = "token",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("ResetPasswordError");
    }

    [Fact]
    public async Task ResetPasswordPost_Success_ClearsLockoutAndRendersTheLoginViewWithSuccessInfo()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager
            .Setup(_ => _.ResetPasswordAsync(user, "token", "NouveauMdp1234"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ResetPassword(new AccountModel
        {
            ResetPasswordUserId = "1",
            ResetPasswordToken = "token",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.SuccessInfo.Should().Be("PasswordReset");
        _userManager.Verify(_ => _.ResetAccessFailedCountAsync(user), Times.Once);
    }

    // ------------------------------------------------------------- ChangeEmail

    [Fact]
    public async Task ChangeEmail_WrongCurrentPassword_SetsInvalidPasswordError()
    {
        SetCurrentUser(1);
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "mauvais")).ReturnsAsync(false);

        var result = await _controller.ChangeEmail(new AccountModel
        {
            PasswordSubmission = "mauvais",
            NewEmailSubmission = "nouveau@kikole.test",
            NewEmailConfirmSubmission = "nouveau@kikole.test"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("InvalidPassword");
    }

    [Fact]
    public async Task ChangeEmail_MismatchedConfirmation_SetsNotMatchingEmailError()
    {
        SetCurrentUser(1);
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "bonmdp")).ReturnsAsync(true);

        var result = await _controller.ChangeEmail(new AccountModel
        {
            PasswordSubmission = "bonmdp",
            NewEmailSubmission = "nouveau@kikole.test",
            NewEmailConfirmSubmission = "autre@kikole.test"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("NotMatchingEmail");
    }

    [Fact]
    public async Task ChangeEmail_AlreadyUsedByAnotherAccount_SetsError()
    {
        SetCurrentUser(1);
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "bonmdp")).ReturnsAsync(true);
        _userRepository
            .Setup(_ => _.GetUserByEmailHashIncludingDisabledAsync("hash:nouveau@kikole.test"))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(2).Build());

        var result = await _controller.ChangeEmail(new AccountModel
        {
            PasswordSubmission = "bonmdp",
            NewEmailSubmission = "nouveau@kikole.test",
            NewEmailConfirmSubmission = "nouveau@kikole.test"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("EmailAlreadyUsed");
    }

    [Fact]
    public async Task ChangeEmail_Success_GeneratesATokenAndSetsSuccessInfo()
    {
        SetCurrentUser(1);
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "bonmdp")).ReturnsAsync(true);
        _userManager.Setup(_ => _.GenerateChangeEmailTokenAsync(user, "nouveau@kikole.test")).ReturnsAsync("token");

        var result = await _controller.ChangeEmail(new AccountModel
        {
            PasswordSubmission = "bonmdp",
            NewEmailSubmission = "nouveau@kikole.test",
            NewEmailConfirmSubmission = "nouveau@kikole.test"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.SuccessInfo.Should().Be("EmailChangeRequested");
        _userManager.Verify(_ => _.GenerateChangeEmailTokenAsync(user, "nouveau@kikole.test"), Times.Once);
        // l'ancienne adresse reste seule active tant que le lien n'est pas suivi
        user.Email.Should().Be("joueur1@kikole.test");
    }

    // ------------------------------------------------------------- Create

    [Fact]
    public async Task Create_LoginTooShort_SetsTooShortLoginError()
    {
        var result = await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "ab",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("TooShortLogin");
    }

    [Fact]
    public async Task Create_LoginAlreadyTaken_SetsAlreadyExistsError()
    {
        _userManager.Setup(_ => _.FindByNameAsync("joueur1")).ReturnsAsync(BuildUser());

        var result = await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "joueur1",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("AlreadyExistsAccount");
    }

    [Fact]
    public async Task Create_InvalidEmailFormat_SetsInvalidEmailError()
    {
        var result = await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "pas-un-email"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("InvalidEmail");
        _userManager.Verify(_ => _.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_BlockedEmailDomain_SetsEmailDomainBlockedError()
    {
        var controller = BuildController(_registrationOptions with
        {
            BlockedEmailDomains = new[] { "yopmail.com" }
        });

        var result = await controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@YopMail.com"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("EmailDomainBlocked");
    }

    [Fact]
    public async Task Create_EmailAlreadyUsed_SetsEmailAlreadyUsedError()
    {
        _userManager.Setup(_ => _.FindByNameAsync("nouveau")).ReturnsAsync((ApplicationUser?)null);
        _userRepository
            .Setup(_ => _.GetUserByEmailHashIncludingDisabledAsync("hash:nouveau@kikole.test"))
            .ReturnsAsync(UserDtoBuilder.Valid().Build());

        var result = await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("EmailAlreadyUsed");
        _userManager.Verify(_ => _.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_Success_LogsInAutomatically()
    {
        // premier appel (Create: "ce login existe-t-il deja ?") -> non ; second appel
        // (LogIn automatique post-creation, meme login) -> l'utilisateur cree
        var createdUser = BuildUser(id: 9, login: "nouveau");
        _userManager.SetupSequence(_ => _.FindByNameAsync("nouveau"))
            .ReturnsAsync((ApplicationUser?)null)
            .ReturnsAsync(createdUser);

        _userManager
            .Setup(_ => _.CreateAsync(It.IsAny<ApplicationUser>(), "NouveauMdp1234"))
            .ReturnsAsync(IdentityResult.Success);

        _signInManager
            .Setup(_ => _.PasswordSignInAsync(createdUser, "NouveauMdp1234", true, true))
            .ReturnsAsync(SignInResult.Success);

        var result = await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test"
        });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "Index" && r.ControllerName == "Home");
        _userManager.Verify(_ => _.CreateAsync(It.IsAny<ApplicationUser>(), "NouveauMdp1234"), Times.Once);
        _userRepository.Verify(_ => _.CreateLoginHistoryAsync(9, It.IsAny<string?>()), Times.Once);
        // developpement local (SendingEnabled=false) : le compte est auto-confirme, jamais envoye
        _emailSender.Verify(_ => _.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenSendingIsEnabled_SendsAConfirmationEmailInsteadOfAutoConfirming()
    {
        var controller = BuildController(_registrationOptions, _emailOptions with { SendingEnabled = true });

        _userManager.Setup(_ => _.FindByNameAsync("nouveau")).ReturnsAsync((ApplicationUser?)null);
        _userManager
            .Setup(_ => _.CreateAsync(It.IsAny<ApplicationUser>(), "NouveauMdp1234"))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((u, _) => u.Id = 9);
        _userManager.Setup(_ => _.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("token");

        await controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test"
        });

        _emailSender.Verify(_ => _.SendAsync("nouveau@kikole.test", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _userManager.Verify(_ => _.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenTheConfirmationEmailCannotBeSent_KeepsTheAccountAndExplains()
    {
        var controller = BuildController(_registrationOptions, _emailOptions with { SendingEnabled = true });

        _userManager.Setup(_ => _.FindByNameAsync("nouveau")).ReturnsAsync((ApplicationUser?)null);
        _userManager
            .Setup(_ => _.CreateAsync(It.IsAny<ApplicationUser>(), "NouveauMdp1234"))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((u, _) => u.Id = 9);
        _userManager.Setup(_ => _.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("token");
        _emailSender
            .Setup(_ => _.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp indisponible"));

        var result = await controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test"
        });

        ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Which.Error.Should().Be("ConfirmationEmailNotSent");
        _userManager.Verify(_ => _.CreateAsync(It.IsAny<ApplicationUser>(), "NouveauMdp1234"), Times.Once);
        _errorJournal.Verify(_ => _.Add(It.IsAny<InvalidOperationException>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    // ------------------------------------------------------------- ResendConfirmation

    private AccountController ControllerWithSending() =>
        BuildController(_registrationOptions, _emailOptions with { SendingEnabled = true });

    private ApplicationUser UnconfirmedUser(ulong id = 5)
    {
        var user = BuildUser(id, "joueur5");
        user.EmailConfirmed = false;
        _userManager.Setup(_ => _.FindByNameAsync("joueur5")).ReturnsAsync(user);
        _userManager.Setup(_ => _.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("token");
        return user;
    }

    private static AccountModel Credentials(string login = "joueur5", string? password = "MotDePasse1234") =>
        new() { LoginSubmission = login, PasswordSubmission = password };

    private static AccountModel ModelOf(IActionResult result) =>
        ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;

    [Fact]
    public async Task ResendConfirmation_WithoutAPassword_IsRefused()
    {
        var result = await ControllerWithSending().ResendConfirmation(Credentials(password: " "));

        ModelOf(result).Error.Should().Be("InvalidForm");
        _emailSender.Verify(_ => _.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmation_WithWrongCredentials_SendsNothing()
    {
        var user = UnconfirmedUser();
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "MotDePasse1234")).ReturnsAsync(false);

        var result = await ControllerWithSending().ResendConfirmation(Credentials());

        ModelOf(result).Error.Should().Be("InvalidCredentials");
        _emailSender.Verify(_ => _.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _userManager.Verify(_ => _.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task ResendConfirmation_ForAnUnknownAccount_AnswersLikeWrongCredentials()
    {
        _userManager.Setup(_ => _.FindByNameAsync("inconnu")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(_ => _.FindByEmailAsync("inconnu")).ReturnsAsync((ApplicationUser?)null);

        var result = await ControllerWithSending().ResendConfirmation(Credentials(login: "inconnu"));

        ModelOf(result).Error.Should().Be("InvalidCredentials");
    }

    [Fact]
    public async Task ResendConfirmation_WhenTheAccountIsLockedOut_SendsNothing()
    {
        var user = UnconfirmedUser();
        _userManager.Setup(_ => _.IsLockedOutAsync(user)).ReturnsAsync(true);

        var result = await ControllerWithSending().ResendConfirmation(Credentials());

        ModelOf(result).Error.Should().Be("AccountLockedOut");
        _emailSender.Verify(_ => _.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmation_WhenAlreadyConfirmed_SendsNothing()
    {
        var user = UnconfirmedUser();
        user.EmailConfirmed = true;
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "MotDePasse1234")).ReturnsAsync(true);

        var result = await ControllerWithSending().ResendConfirmation(Credentials());

        ModelOf(result).SuccessInfo.Should().Be("ConfirmationEmailAlreadyConfirmed");
        _emailSender.Verify(_ => _.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmation_SendsOnceThenMakesYouWait()
    {
        var user = UnconfirmedUser();
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "MotDePasse1234")).ReturnsAsync(true);
        var controller = ControllerWithSending();

        // le ViewData du controleur est partage : on lit chaque modele juste apres son appel
        var first = ModelOf(await controller.ResendConfirmation(Credentials()));
        var second = ModelOf(await controller.ResendConfirmation(Credentials()));

        first.SuccessInfo.Should().Be("ConfirmationEmailResent");
        second.Error.Should().Be("ConfirmationEmailThrottled");
        _emailSender.Verify(_ => _.SendAsync(user.Email!, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ResendConfirmation_WhenSendingFails_ReportsItAndAllowsAnotherTry()
    {
        var user = UnconfirmedUser();
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "MotDePasse1234")).ReturnsAsync(true);
        _emailSender
            .SetupSequence(_ => _.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp indisponible"))
            .Returns(Task.CompletedTask);
        var controller = ControllerWithSending();

        var first = ModelOf(await controller.ResendConfirmation(Credentials()));
        var second = ModelOf(await controller.ResendConfirmation(Credentials()));

        first.Error.Should().Be("ConfirmationEmailNotSent");
        second.SuccessInfo.Should().Be("ConfirmationEmailResent");
        _errorJournal.Verify(_ => _.Add(It.IsAny<InvalidOperationException>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    // ------------------------------------------------------------- Create (parrainage)

    private void SetupPlainCreate(string login = "nouveau")
    {
        _userManager.Setup(_ => _.FindByNameAsync(login)).ReturnsAsync((ApplicationUser?)null);
        _userManager
            .Setup(_ => _.CreateAsync(It.IsAny<ApplicationUser>(), "NouveauMdp1234"))
            .ReturnsAsync(IdentityResult.Success);
        // le LogIn automatique post-creation echoue silencieusement (non teste ici) : evite
        // d'avoir a mocker un troisieme scenario juste pour ces tests de resolution du parrain
        _signInManager
            .Setup(_ => _.PasswordSignInAsync(It.IsAny<ApplicationUser>(), "NouveauMdp1234", true, true))
            .ReturnsAsync(SignInResult.Failed);
    }

    [Fact]
    public async Task Create_WithAValidSponsor_SetsSponsorUserId()
    {
        SetupPlainCreate();
        var sponsor = BuildUser(id: 42, login: "parrain1");
        _userManager.Setup(_ => _.FindByNameAsync("parrain1")).ReturnsAsync(sponsor);

        await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test",
            SponsorLoginSubmission = "parrain1"
        });

        _userManager.Verify(_ => _.CreateAsync(
            It.Is<ApplicationUser>(u => u.SponsorUserId == 42UL), "NouveauMdp1234"), Times.Once);
    }

    [Fact]
    public async Task Create_WithAnUnknownSponsorLogin_SilentlyLeavesSponsorUserIdNull()
    {
        SetupPlainCreate();
        _userManager.Setup(_ => _.FindByNameAsync("inconnu")).ReturnsAsync((ApplicationUser?)null);

        await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test",
            SponsorLoginSubmission = "inconnu"
        });

        _userManager.Verify(_ => _.CreateAsync(
            It.Is<ApplicationUser>(u => u.SponsorUserId == null), "NouveauMdp1234"), Times.Once);
    }

    [Fact]
    public async Task Create_WithADisabledSponsor_SilentlyLeavesSponsorUserIdNull()
    {
        SetupPlainCreate();
        var sponsor = BuildUser(id: 42, login: "parrain1");
        sponsor.IsDisabled = true;
        _userManager.Setup(_ => _.FindByNameAsync("parrain1")).ReturnsAsync(sponsor);

        await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test",
            SponsorLoginSubmission = "parrain1"
        });

        _userManager.Verify(_ => _.CreateAsync(
            It.Is<ApplicationUser>(u => u.SponsorUserId == null), "NouveauMdp1234"), Times.Once);
    }

    [Fact]
    public async Task Create_WithSelfAsSponsorDifferentCase_SilentlyLeavesSponsorUserIdNull()
    {
        SetupPlainCreate();
        // "NOUVEAU" (le parrain saisi) et "nouveau" (le login choisi) doivent etre reconnus
        // comme le meme compte via le normaliseur (Sanitize + majuscules), pas une simple
        // comparaison de chaines
        // NormalizedUserName pose explicitement : en production, DapperUserStore.ToUser le
        // renseigne toujours depuis UserDto.NormalizedLogin ; BuildUser() ne le fait pas par
        // defaut (aucun autre test existant n'en a besoin).
        var self = BuildUser(id: 9, login: "NOUVEAU");
        self.NormalizedUserName = "NOUVEAU";
        _userManager.Setup(_ => _.FindByNameAsync("NOUVEAU")).ReturnsAsync(self);

        await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test",
            SponsorLoginSubmission = "NOUVEAU"
        });

        _userManager.Verify(_ => _.CreateAsync(
            It.Is<ApplicationUser>(u => u.SponsorUserId == null), "NouveauMdp1234"), Times.Once);
    }

    [Fact]
    public async Task Create_WithSponsorshipDisabled_SilentlyLeavesSponsorUserIdNullEvenWithAValidSponsor()
    {
        SetupPlainCreate();
        var sponsor = BuildUser(id: 42, login: "parrain1");
        _userManager.Setup(_ => _.FindByNameAsync("parrain1")).ReturnsAsync(sponsor);
        var controller = BuildController(_registrationOptions with { SponsorshipEnabled = false });

        await controller.Create(new AccountModel
        {
            LoginCreateSubmission = "nouveau",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234",
            EmailCreateSubmission = "nouveau@kikole.test",
            SponsorLoginSubmission = "parrain1"
        });

        _userManager.Verify(_ => _.CreateAsync(
            It.Is<ApplicationUser>(u => u.SponsorUserId == null), "NouveauMdp1234"), Times.Once);
        _badgeService.Verify(_ => _.PrepareSponsorshipBadgesAsync(It.IsAny<ulong>(), It.IsAny<Languages>()), Times.Never);
    }

    // ------------------------------------------------------------- Index (parrainage)

    [Fact]
    public async Task Index_Get_AuthenticatedWithoutSponsorshipInfo_LeavesTheSectionEmpty()
    {
        SetCurrentUser(1);
        var me = BuildUser(id: 1, login: "joueur1");
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(me);

        var result = await _controller.Index();

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.HasSponsorshipInfo.Should().BeFalse();
    }

    [Fact]
    public async Task Index_Get_AuthenticatedWithSponsorAndGodchildren_PopulatesTheSponsorshipSection()
    {
        SetCurrentUser(1);
        var me = BuildUser(id: 1, login: "joueur1");
        me.SponsorUserId = 42;
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(me);
        _userRepository
            .Setup(_ => _.GetUserByIdIncludingDisabledAsync(42))
            .ReturnsAsync(UserDtoBuilder.Valid().WithId(42).WithLogin("parrain1").Build());
        _userRepository
            .Setup(_ => _.GetGodchildrenAsync(1))
            .ReturnsAsync(new[]
            {
                UserDtoBuilder.Valid().WithId(2).WithLogin("filleul1").Build(),
                UserDtoBuilder.Valid().WithId(3).WithLogin("filleul2").WithDisabled().Build()
            });

        var result = await _controller.Index();

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.HasSponsorshipInfo.Should().BeTrue();
        model.SponsorLogin.Should().Be("parrain1");
        model.Godchildren.Should().BeEquivalentTo(new[] { ("filleul1", false), ("filleul2", true) });
    }

    [Fact]
    public async Task Index_Get_WithSponsorshipDisabled_LeavesTheSectionEmptyEvenWithExistingSponsorAndGodchildren()
    {
        SetCurrentUser(1);
        var me = BuildUser(id: 1, login: "joueur1");
        me.SponsorUserId = 42;
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(me);
        var controller = BuildController(_registrationOptions with { SponsorshipEnabled = false });

        var result = await controller.Index();

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.HasSponsorshipInfo.Should().BeFalse();
        model.SponsorLogin.Should().BeNull();
        model.Godchildren.Should().BeEmpty();
        _userRepository.Verify(_ => _.GetUserByIdIncludingDisabledAsync(It.IsAny<ulong>()), Times.Never);
        _userRepository.Verify(_ => _.GetGodchildrenAsync(It.IsAny<ulong>()), Times.Never);
    }

    // ------------------------------------------------------------- ChangePassword

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_SetsResetPasswordError()
    {
        SetCurrentUser(1);
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager
            .Setup(_ => _.ChangePasswordAsync(user, "mauvais", "NouveauMdp1234"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = nameof(IdentityErrorDescriber.PasswordMismatch) }));

        var result = await _controller.ChangePassword(new AccountModel
        {
            PasswordSubmission = "mauvais",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("PasswordDoesNotMatch");
    }

    [Fact]
    public async Task ChangePassword_Success_SetsSuccessInfo()
    {
        SetCurrentUser(1);
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager
            .Setup(_ => _.ChangePasswordAsync(user, "ancien", "NouveauMdp1234"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ChangePassword(new AccountModel
        {
            PasswordSubmission = "ancien",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.SuccessInfo.Should().Be("PasswordChanged");
    }

    // ------------------------------------------------------------- helpers

    private void SetCurrentUser(ulong userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);
    }
}
