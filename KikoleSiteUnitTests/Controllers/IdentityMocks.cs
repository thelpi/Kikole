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

    internal static SignInManager<ApplicationUser> MockSignInManager()
    {
        var userManager = MockUserManager();
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        var signInManager = new Mock<SignInManager<ApplicationUser>>(
            userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

        return signInManager.Object;
    }
}
