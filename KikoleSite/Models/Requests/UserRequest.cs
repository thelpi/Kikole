using KikoleSite.Helpers;
using KikoleSite.Identity;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models.Requests;

public record UserRequest
{
    public required string Login { get; init; }

    public required string Password { get; init; }

    public required string Email { get; init; }

    public Languages? Language { get; init; }

    public required string? Ip { get; init; }

    /// <summary>
    /// Deja resolu (et valide - existe, pas desactive, pas soi-meme) par l'appelant :
    /// ce record ne fait aucun acces aux donnees, cf. le principe deja suivi ailleurs
    /// dans le projet.
    /// </summary>
    public ulong? SponsorUserId { get; init; }

    internal ApplicationUser ToApplicationUser()
    {
        return new ApplicationUser
        {
            UserName = Login.Sanitize(),
            Email = Email,
            EmailConfirmed = false,
            LanguageId = (ulong)(Language ?? Languages.en),
            UserType = UserTypes.StandardUser,
            Ip = Ip,
            SponsorUserId = SponsorUserId
        };
    }
}
