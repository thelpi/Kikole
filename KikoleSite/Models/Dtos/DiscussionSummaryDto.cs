using System;

namespace KikoleSite.Models.Dtos;

public record DiscussionSummaryDto
{
    public required ulong DiscussionId { get; init; }

    public required ulong UserId { get; init; }

    public required string UserLogin { get; init; }

    public DateTime? LastMessageDate { get; init; }

    public bool HasUnreadFromUser { get; init; }
}
