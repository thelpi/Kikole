using System;

namespace KikoleSite.Models.Dtos;

public record DiscussionDto
{
    public ulong Id { get; init; }

    public required ulong UserId { get; init; }

    public DateTime CreationDate { get; init; }
}
