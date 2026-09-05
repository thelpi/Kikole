namespace KikoleSite.Models.Dtos;

public record BadgeDto : BaseDto
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public byte Hidden { get; init; }
}
