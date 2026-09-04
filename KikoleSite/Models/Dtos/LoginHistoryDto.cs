using System;

namespace KikoleSite.Models.Dtos;

public record LoginHistoryDto
{
    public ulong Id { get; init; }

    public ulong UserId { get; init; }

    public string? Ip { get; init; }

    public DateTime CreationDate { get; init; }
}
