namespace KikoleSite.Models.Dtos;

public record DiscussionDto : BaseDto
{
    public ulong UserId { get; init; }

    public required string Email { get; init; }

    public required string Message { get; init; }
}
