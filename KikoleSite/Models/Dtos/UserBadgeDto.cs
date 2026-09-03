using System;

namespace KikoleSite.Models.Dtos;

public record UserBadgeDto
{
    public ulong UserId { get; init; }

    public ulong BadgeId { get; init; }

    public DateTime GetDate { get; init; }
}
