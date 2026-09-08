using System.Collections.Generic;

namespace KikoleSite.ViewModels;

public class AccountModel
{
    public bool IsAuthenticated { get; set; }

    public string? Login { get; set; }

    public string? LoginSubmission { get; set; }
    public string? PasswordSubmission { get; set; }

    public string? LoginCreateSubmission { get; set; }
    public string? PasswordCreate1Submission { get; set; }
    public string? PasswordCreate2Submission { get; set; }
    public string? RecoveryQCreate { get; set; }
    public string? RecoveryACreate { get; set; }

    /// <summary>Login du parrain saisi a l'inscription (facultatif) - ignore en silence
    /// s'il ne correspond a personne d'eligible, cf. <c>AccountController.Create</c>.</summary>
    public string? SponsorLoginSubmission { get; set; }

    public string? Error { get; set; }
    public string? SuccessInfo { get; set; }


    public string? LoginRecoverySubmission { get; set; }
    public string? QuestionRecovery { get; set; }

    public string? RegistrationId { get; set; }

    public bool RegistrationInviteEnabled { get; set; }

    /// <summary>Login du parrain de l'utilisateur connecte, ou <c>null</c> si non parraine.</summary>
    public string? SponsorLogin { get; set; }

    public IReadOnlyList<(string Login, bool IsDisabled)> Godchildren { get; set; } = [];

    /// <summary>La section parrainage ne s'affiche que si elle a quelque chose a montrer.</summary>
    public bool HasSponsorshipInfo => SponsorLogin != null || Godchildren.Count > 0;
}
