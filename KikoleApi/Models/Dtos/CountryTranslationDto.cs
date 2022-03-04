namespace KikoleApi.Models.Dtos
{
    internal class CountryTranslationDto
    {
        public ulong CountryId { get; set; }

        public ulong LanguageId { get; set; }

        public string Name { get; set; }
    }
}
