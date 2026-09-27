namespace KikoleSite.ViewModels.Emails;

/// <summary>
/// Modele partage par les trois emails a lien (confirmation d'inscription, changement
/// d'adresse, reinitialisation de mot de passe) : meme structure a chaque fois (une
/// introduction, un lien, une mention de duree de validite), cf. <c>Views/Emails/LinkEmail.cshtml</c>.
/// </summary>
public record LinkEmailModel
{
    public required string Introduction { get; init; }

    public required string Link { get; init; }

    public required string ValidityNotice { get; init; }
}
