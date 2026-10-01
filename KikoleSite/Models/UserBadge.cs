using System;

namespace KikoleSite.Models;

public class UserBadge
{
    private readonly Badge _badge;

    public ulong Id => _badge.Id;

    public string Name => _badge.Name;

    public string Description => _badge.Description;

    public int Users => _badge.Users;

    public bool Hidden => _badge.Hidden;

    public DateOnly GetDate { get; }

    internal UserBadge(Badge badge, DateOnly getDate)
    {
        _badge = badge;
        GetDate = getDate;
    }
}
