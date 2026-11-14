using System;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.ViewModels;

public class UserAdminRow
{
    public ulong Id { get; init; }

    public required string Login { get; init; }

    public required string Email { get; init; }

    public DateTime CreationDate { get; init; }

    public string? Ip { get; init; }

    public bool IsDisabled { get; init; }

    /// <summary>L'adresse email n'a jamais ete confirmee (lien du mail d'inscription jamais ouvert).</summary>
    public bool IsEmailUnconfirmed { get; init; }

    /// <summary>L'utilisateur a demande la suppression de son compte (en attente de traitement).</summary>
    public bool DeletionRequested { get; init; }

    public DateTime? DeletionRequestedDate { get; init; }

    public string? DeletionReason { get; init; }

    public UserTypes UserType { get; init; }

    public static UserAdminRow From(UserDto user, string email) => new()
    {
        Id = user.Id,
        Login = user.Login,
        Email = email,
        CreationDate = user.CreationDate,
        Ip = user.Ip,
        IsDisabled = user.IsDisabled,
        IsEmailUnconfirmed = !user.EmailConfirmed,
        DeletionRequested = user.DeletionRequested,
        DeletionRequestedDate = user.DeletionRequestedDate,
        DeletionReason = user.DeletionReason,
        UserType = (UserTypes)user.UserTypeId
    };
}
