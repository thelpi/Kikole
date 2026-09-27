using System.Collections.Generic;

namespace KikoleSite.ViewModels;

public class AccountModel
{
    public bool IsAuthenticated { get; set; }

    public string? Login { get; set; }

    /// <summary>Adresse email de l'utilisateur connecte (dechiffree), pour affichage dans "Mon compte".</summary>
    public string? Email { get; set; }

    public string? LoginSubmission { get; set; }
    public string? PasswordSubmission { get; set; }

    public string? LoginCreateSubmission { get; set; }
    public string? PasswordCreate1Submission { get; set; }
    public string? PasswordCreate2Submission { get; set; }
    public string? EmailCreateSubmission { get; set; }

    /// <summary>Login du parrain saisi a l'inscription (facultatif) - ignore en silence
    /// s'il ne correspond a personne d'eligible, cf. <c>AccountController.Create</c>.</summary>
    public string? SponsorLoginSubmission { get; set; }

    public string? Error { get; set; }
    public string? SuccessInfo { get; set; }

    /// <summary>Email saisi sur le formulaire "mot de passe oublie".</summary>
    public string? PasswordResetEmailSubmission { get; set; }

    /// <summary>Identifiant et jeton portes par le lien de reinitialisation (champs caches du formulaire dedie).</summary>
    public string? ResetPasswordUserId { get; set; }
    public string? ResetPasswordToken { get; set; }

    /// <summary>Nouvelle adresse email demandee depuis "Mon compte" (avec confirmation).</summary>
    public string? NewEmailSubmission { get; set; }
    public string? NewEmailConfirmSubmission { get; set; }

    public string? RegistrationId { get; set; }

    public bool RegistrationInviteEnabled { get; set; }

    /// <summary>Systeme de parrainage actif (config <c>Registration:SponsorshipEnabled</c>) -
    /// desactive, ni le champ d'inscription ni la section "Mon compte" ne s'affichent.</summary>
    public bool SponsorshipEnabled { get; set; }

    /// <summary>Login du parrain de l'utilisateur connecte, ou <c>null</c> si non parraine.</summary>
    public string? SponsorLogin { get; set; }

    public IReadOnlyList<(string Login, bool IsDisabled)> Godchildren { get; set; } = [];

    /// <summary>La section parrainage ne s'affiche que si le systeme est actif et qu'elle
    /// a quelque chose a montrer.</summary>
    public bool HasSponsorshipInfo => SponsorshipEnabled && (SponsorLogin != null || Godchildren.Count > 0);
}
