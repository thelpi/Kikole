namespace KikoleSite.Models.Dtos;

public record RegistrationGuidDto
{
    public required string Id { get; init; }
    public ulong? UserId { get; init; }
}
