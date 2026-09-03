using System;

namespace KikoleSite.Models.Dtos;

public record MessageDto : BaseDto
{
    public required string Message { get; init; }

    public DateTime? DisplayFrom { get; init; }

    public DateTime? DisplayTo { get; init; }
}
