using System;
using KikoleSite.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace KikoleSite.Identity;

/// <summary>
/// Utilisateur Identity. Herite de <see cref="IdentityUser{TKey}"/> pour recuperer
/// gratuitement Id/UserName/PasswordHash/ConcurrencyStamp/verrouillage ; les champs
/// propres au jeu (type d'utilisateur, question de recuperation...) sont ajoutes ici.
/// </summary>
public class ApplicationUser : IdentityUser<ulong>
{
    public UserTypes UserType { get; set; }

    public ulong LanguageId { get; set; }

    public required string PasswordResetQuestion { get; set; }

    public required string PasswordResetAnswerHash { get; set; }

    public string? Ip { get; set; }

    public bool IsDisabled { get; set; }

    public DateTime CreationDate { get; set; }
}
