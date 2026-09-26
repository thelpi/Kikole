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
using Microsoft.Extensions.Localization;
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
    private readonly Mock<IPasswordHasher<ApplicationUser>> _passwordHasher = new();
    private readonly RegistrationOptions _registrationOptions = new() { SponsorshipEnabled = true };
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _signInManager = IdentityMocks.MockSignInManager(_userManager);

        _localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // RenderIndexAsync l'appelle systematiquement des qu'un utilisateur est connecte
        // (section parrainage) : par defaut, aucun filleul, comme pour les autres mocks
        // "silencieux" ci-dessus
        _userRepository.Setup(_ => _.GetGodchildrenAsync(It.IsAny<ulong>())).ReturnsAsync(new List<UserDto>());

        _controller = new AccountController(
            _localizer.Object,
            _userManager.Object,
            _signInManager.Object,
            _passwordHasher.Object,
            new SanitizingLookupNormalizer(),
            new OptionsWrapper<RegistrationOptions>(_registrationOptions),
            _userRepository.Object,
            _internationalService.Object,
            _clock.Object,
            _gameCalendar.Object,
            _playerService.Object,
            _badgeService.Object,
            httpContextAccessor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    /// <summary>Instance dediee pour les tests qui ont besoin d'une config differente de
    /// celle par defaut (<see cref="_registrationOptions"/> est immuable une fois passee
    /// a <see cref="_controller"/>, cf. <c>init</c> sur <see cref="RegistrationOptions"/>).</summary>
    private AccountController BuildController(RegistrationOptions options)
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);

        return new AccountController(
            _localizer.Object,
            _userManager.Object,
            _signInManager.Object,
            _passwordHasher.Object,
            new SanitizingLookupNormalizer(),
            new OptionsWrapper<RegistrationOptions>(options),
            _userRepository.Object,
            _internationalService.Object,
            _clock.Object,
            _gameCalendar.Object,
            _playerService.Object,
            _badgeService.Object,
            httpContextAccessor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    private static ApplicationUser BuildUser(ulong id = 1, string login = "joueur1")
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = login,
            PasswordResetQuestion = "une question ?",
            PasswordResetAnswerHash = "hash-reponse"
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

    // ------------------------------------------------------------- GetLoginQuestion

    [Fact]
    public async Task GetLoginQuestion_UnknownUser_SetsUserDoesNotExistError()
    {
        _userManager.Setup(_ => _.FindByNameAsync("fantome")).ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.GetLoginQuestion(new AccountModel { LoginRecoverySubmission = "fantome" });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("UserDoesNotExist");
    }

    [Fact]
    public async Task GetLoginQuestion_KnownUser_ExposesTheRecoveryQuestion()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByNameAsync("joueur1")).ReturnsAsync(user);

        var result = await _controller.GetLoginQuestion(new AccountModel { LoginRecoverySubmission = "joueur1" });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.QuestionRecovery.Should().Be("une question ?");
    }

    // ------------------------------------------------------------- ResetPassword

    [Fact]
    public async Task ResetPassword_UnknownUser_SetsResetPasswordError()
    {
        _userManager.Setup(_ => _.FindByNameAsync("fantome")).ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.ResetPassword(new AccountModel
        {
            LoginRecoverySubmission = "fantome",
            RecoveryACreate = "reponse",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("ResetPasswordError");
    }

    [Fact]
    public async Task ResetPassword_Success_SetsSuccessInfo()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByNameAsync("joueur1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.IsLockedOutAsync(user)).ReturnsAsync(false);
        _passwordHasher
            .Setup(_ => _.VerifyHashedPassword(user, user.PasswordResetAnswerHash, "reponse"))
            .Returns(PasswordVerificationResult.Success);
        _userManager.Setup(_ => _.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
        _userManager
            .Setup(_ => _.ResetPasswordAsync(user, "token", "NouveauMdp1234"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ResetPassword(new AccountModel
        {
            LoginRecoverySubmission = "joueur1",
            RecoveryACreate = "reponse",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.SuccessInfo.Should().Be("PasswordReset");
        model.Error.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_WrongSecurityAnswer_RecordsAccessFailedAndSetsError()
    {
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByNameAsync("joueur1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.IsLockedOutAsync(user)).ReturnsAsync(false);
        _passwordHasher
            .Setup(_ => _.VerifyHashedPassword(user, user.PasswordResetAnswerHash, "mauvaise"))
            .Returns(PasswordVerificationResult.Failed);

        var result = await _controller.ResetPassword(new AccountModel
        {
            LoginRecoverySubmission = "joueur1",
            RecoveryACreate = "mauvaise",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("ResetPasswordError");
        _userManager.Verify(_ => _.AccessFailedAsync(user), Times.Once);
    }

    // ------------------------------------------------------------- ResetQAndA

    [Fact]
    public async Task ResetQAndA_WrongCurrentPassword_SetsInvalidPasswordError()
    {
        SetCurrentUser(1);
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "mauvais")).ReturnsAsync(false);

        var result = await _controller.ResetQAndA(new AccountModel
        {
            PasswordSubmission = "mauvais",
            RecoveryQCreate = "question",
            RecoveryACreate = "reponse"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("InvalidPassword");
        _userManager.Verify(_ => _.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ResetQAndA_Success_UpdatesTheUserAndSetsSuccessInfo()
    {
        SetCurrentUser(1);
        var user = BuildUser();
        _userManager.Setup(_ => _.FindByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(_ => _.CheckPasswordAsync(user, "bonmdp")).ReturnsAsync(true);
        _passwordHasher.Setup(_ => _.HashPassword(user, "nouvelle-reponse")).Returns("nouveau-hash");
        _userManager.Setup(_ => _.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ResetQAndA(new AccountModel
        {
            PasswordSubmission = "bonmdp",
            RecoveryQCreate = "nouvelle question",
            RecoveryACreate = "nouvelle-reponse"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.SuccessInfo.Should().Be("QandAUpdated");
        user.PasswordResetQuestion.Should().Be("nouvelle question");
        user.PasswordResetAnswerHash.Should().Be("nouveau-hash");
    }

    // ------------------------------------------------------------- Create

    [Fact]
    public async Task Create_LoginTooShort_SetsTooShortLoginError()
    {
        var result = await _controller.Create(new AccountModel
        {
            LoginCreateSubmission = "ab",
            PasswordCreate1Submission = "NouveauMdp1234",
            PasswordCreate2Submission = "NouveauMdp1234"
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
            PasswordCreate2Submission = "NouveauMdp1234"
        });

        var model = ((ViewResult)result).Model.Should().BeOfType<AccountModel>().Subject;
        model.Error.Should().Be("AlreadyExistsAccount");
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
            PasswordCreate2Submission = "NouveauMdp1234"
        });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.Should().Match<RedirectToActionResult>(r => r.ActionName == "Index" && r.ControllerName == "Home");
        _userManager.Verify(_ => _.CreateAsync(It.IsAny<ApplicationUser>(), "NouveauMdp1234"), Times.Once);
        _userRepository.Verify(_ => _.CreateLoginHistoryAsync(9, It.IsAny<string?>()), Times.Once);
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
