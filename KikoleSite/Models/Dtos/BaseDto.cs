using System;

namespace KikoleSite.Models.Dtos;

public abstract record BaseDto
{
    public ulong Id { get; init; }

    public DateTime CreationDate { get; init; }

    public DateTime UpdateDate { get; init; }
}
