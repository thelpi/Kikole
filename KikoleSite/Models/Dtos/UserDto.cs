using System;

namespace KikoleSite.Models.Dtos;

public record UserDto : BaseDto
{
    public required string Login { get; init; }

    public required string NormalizedLogin { get; init; }

    public required string Password { get; init; }

    /// <summary>Adresse email chiffree (AES-GCM) - lisible uniquement via <see cref="KikoleSite.Identity.IEmailProtector.Decrypt"/>, pour l'exploitation en cas de fraude.</summary>
    public required string EmailEncrypted { get; init; }

    /// <summary>Empreinte deterministe (HMAC-SHA256) de l'adresse normalisee - sert a la recherche, l'unicite et la connexion par email sans dechiffrer.</summary>
    public required string EmailHash { get; init; }

    public bool EmailConfirmed { get; init; }

    public ulong LanguageId { get; init; }

    public ulong UserTypeId { get; init; }

    public string? Ip { get; init; }

    public bool IsDisabled { get; init; }

    public required string ConcurrencyStamp { get; init; }

    public required string SecurityStamp { get; init; }

    public DateTime? LockoutEnd { get; init; }

    public int AccessFailedCount { get; init; }

    public bool LockoutEnabled { get; init; }

    /// <summary>
    /// Utilisateur ayant parraine celui-ci a l'inscription (login saisi dans le formulaire,
    /// resolu et fige a la creation du compte - ne change jamais ensuite). <c>Null</c> si
    /// non parraine, ou si le login saisi ne correspondait a personne d'eligible.
    /// </summary>
    public ulong? SponsorUserId { get; init; }
}
