namespace KikoleSite.Configuration;

/// <summary>
/// Section <c>Email</c> : parametres d'envoi et duree de vie des liens (confirmation
/// d'inscription, changement d'adresse, mot de passe oublie - un seul delai partage pour
/// les trois, cf. <c>AccountController</c>).
/// </summary>
public record EmailOptions
{
    /// <summary>
    /// Envoi reel desactive (cf. appsettings.Development.json) : le lien est ecrit dans les
    /// logs plutot qu'envoye, et l'inscription s'auto-confirme immediatement (voir
    /// <c>AccountController.Create</c>) - pas besoin d'une vraie boite mail pour developper
    /// en local.
    /// </summary>
    public bool SendingEnabled { get; init; } = true;

    public required string FromAddress { get; init; }

    public string FromName { get; init; } = "Kikolé";

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; } = 587;

    public string? SmtpUser { get; init; }

    public string? SmtpPassword { get; init; }

    public bool SmtpUseSsl { get; init; } = true;

    /// <summary>
    /// Duree de validite (en heures) des liens de confirmation d'inscription, de
    /// changement d'adresse et de reinitialisation de mot de passe.
    /// </summary>
    public int TokenLifetimeHours { get; init; } = 24;
}
