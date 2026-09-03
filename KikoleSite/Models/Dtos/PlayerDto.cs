using System;

namespace KikoleSite.Models.Dtos;

public record PlayerDto : BaseDto
{
    public required string Name { get; init; }

    public required string AllowedNames { get; init; }

    public ushort YearOfBirth { get; init; }

    public ulong ContinentId { get; init; }

    public ulong CountryId { get; init; }

    public DateTime? PublicationDate { get; init; }

    public required string Clue { get; init; }

    public required string EasyClue { get; init; }

    public ulong? BadgeId { get; init; }

    public ulong PositionId { get; init; }

    public ulong CreationUserId { get; init; }

    public DateTime? RejectDate { get; init; }

    public byte HideCreator { get; init; }
}
