namespace KikoleApi.Domain.Models.Dtos
{
    internal class CountryTranslationDto
    {
        public long CountryId { get; set; }

        public long LanguageId { get; set; }

        public string Name { get; set; }
    }
}
