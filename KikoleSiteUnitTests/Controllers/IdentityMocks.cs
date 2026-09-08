using KikoleSite.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace KikoleSiteUnitTests.Controllers;

/// <summary>
/// <see cref="UserManager{TUser}"/>/<see cref="SignInManager{TUser}"/> ne peuvent pas etre
/// mockes via une interface (Identity les expose comme des classes concretes) : la
/// technique standard est de les construire avec des dependances bouchon, puisque la
/// quasi-totalite de leurs membres sont <c>virtual</c> et donc mockables par Moq malgre
/// tout. C'est ce qui a fait repousser les tests de <see cref="KikoleSite.Controllers.HomeController"/>
/// jusqu'ici (seul <c>SignInManager</c> y est injecte, jamais appele par les actions
/// testees, mais un objet valide reste necessaire pour construire le controleur).
/// </summary>
internal static class IdentityMocks
{
    internal static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
    }

    /// <summary>Variante "consommateur" : le controleur ne fait qu'y injecter l'objet,
    /// sans jamais appeler ses methodes dans les actions testees (cf. HomeController).</summary>
    internal static SignInManager<ApplicationUser> MockSignInManager()
    {
        return MockSignInManager(MockUserManager()).Object;
    }

    /// <summary>Variante "configurable" : le controleur appelle reellement ses methodes
    /// (cf. AccountController) - l'appelant garde la main sur le mock pour poser des
    /// <c>Setup</c>/<c>Verify</c>.</summary>
    internal static Mock<SignInManager<ApplicationUser>> MockSignInManager(Mock<UserManager<ApplicationUser>> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        return new Mock<SignInManager<ApplicationUser>>(
            userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
    }
}
