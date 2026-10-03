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

    public UserTypes UserType { get; init; }

    public static UserAdminRow From(UserDto user, string email) => new()
    {
        Id = user.Id,
        Login = user.Login,
        Email = email,
        CreationDate = user.CreationDate,
        Ip = user.Ip,
        IsDisabled = user.IsDisabled,
        UserType = (UserTypes)user.UserTypeId
    };
}
