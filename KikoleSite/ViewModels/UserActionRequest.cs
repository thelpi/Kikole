using KikoleSite.Models.Enums;

namespace KikoleSite.ViewModels;

/// <summary>Action d'administration sur un utilisateur, avec les critères de liste à restituer ensuite.</summary>
public class UserActionRequest : UserListQuery
{
    public ulong UserId { get; set; }

    public string? Reason { get; set; }

    public string? NewPassword { get; set; }

    public string? NewPasswordConfirm { get; set; }

    public string? LoginConfirmation { get; set; }

    public UserTypes? NewType { get; set; }
}
