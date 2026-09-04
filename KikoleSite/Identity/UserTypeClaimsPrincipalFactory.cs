using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KikoleSite.Identity;

/// <summary>
/// Ajoute au principal genere a la connexion une claim portant le palier utilisateur, pour
/// que l'autorisation par palier (<see cref="MinimumUserTypeHandler"/>) se fasse sur le
/// cookie signe plutot que par un aller-retour base a chaque requete.
/// </summary>
public class UserTypeClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public const string UserTypeClaimType = "user_type";

    public UserTypeClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    { }

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);

        ((ClaimsIdentity)principal.Identity!)
            .AddClaim(new Claim(UserTypeClaimType, ((ulong)user.UserType).ToString()));

        return principal;
    }
}
