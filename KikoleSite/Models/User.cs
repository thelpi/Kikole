using KikoleSite.Models.Dtos;

namespace KikoleSite.Models;

public class User
{
    public ulong Id { get; }

    public string Login { get; }

    internal User(UserDto user)
        : this(user.Id, user.Login)
    { }

    /// <summary>
    /// Pour les cas ou seuls l'identifiant et le login sont connus, sans DTO complet.
    /// </summary>
    internal User(ulong id, string login)
    {
        Id = id;
        Login = login;
    }
}
