using KikoleSite.Models.Enums;

namespace KikoleSite.ViewModels;

/// <summary>Critères de la liste des utilisateurs (query string, ou champs cachés des formulaires d'action).</summary>
public class UserListQuery
{
    public string? Login { get; set; }

    public UserStatusFilter Status { get; set; }

    public UserTypes? Type { get; set; }

    public bool Desc { get; set; }

    public int Page { get; set; } = 1;
}
