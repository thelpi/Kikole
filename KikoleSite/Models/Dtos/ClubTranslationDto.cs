namespace KikoleSite.Models.Dtos
{
    public record ClubTranslationDto
    {
        public ulong ClubId { get; init; }

        public ulong LanguageId { get; init; }

        public byte Priority { get; init; }

        public required string Name { get; init; }
    }
}
