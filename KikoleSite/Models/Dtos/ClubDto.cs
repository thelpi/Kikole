namespace KikoleSite.Models.Dtos
{
    public record ClubDto : BaseDto
    {
        public required string Name { get; init; }

        public ulong CountryId { get; init; }
    }
}
