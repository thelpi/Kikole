using System;

namespace KikoleSite.Models.Dtos;

public record LeaderDto : BaseDto
{
    public ulong UserId { get; init; }

    public DateOnly ProposalDate { get; init; }

    public ushort Points { get; init; }

    public int Time { get; init; }

    internal bool IsCurrentDay => ProposalDate == DateOnly.FromDateTime(CreationDate);
}
