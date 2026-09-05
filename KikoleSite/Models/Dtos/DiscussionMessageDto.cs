using System;

namespace KikoleSite.Models.Dtos;

public record DiscussionMessageDto
{
    public ulong Id { get; init; }

    public required ulong DiscussionId { get; init; }

    public required string Message { get; init; }

    public DateTime CreationDate { get; init; }

    public bool IsFromAdmin { get; init; }

    public bool IsRead { get; init; }
}
