using System;

namespace KikoleSite.Models.Dtos;

public record ProposalDto : BaseDto
{
    public ulong UserId { get; init; }

    public ulong ProposalTypeId { get; init; }

    public string? Value { get; init; }

    public byte Successful { get; init; }

    public DateTime ProposalDate { get; init; }

    public string? Ip { get; init; }

    internal bool IsCurrentDay => ProposalDate == CreationDate.Date;
}
