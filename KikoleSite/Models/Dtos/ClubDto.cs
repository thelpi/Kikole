namespace KikoleSite.Models.Dtos
{
    public record ClubDto : BaseDto
    {
        public required string Name { get; init; }

        public required string AllowedNames { get; init; }

        public ulong CountryId { get; init; }
    }
}
