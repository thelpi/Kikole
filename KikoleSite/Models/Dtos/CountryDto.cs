namespace KikoleSite.Models.Dtos;

public record CountryDto : BaseDto
{
    public required string Code { get; init; }

    public ulong ContinentId { get; init; }

    public required string Name { get; init; }
}
