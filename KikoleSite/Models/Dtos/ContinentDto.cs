namespace KikoleSite.Models.Dtos;

public record ContinentDto : BaseDto
{
    public required string Name { get; init; }
}
