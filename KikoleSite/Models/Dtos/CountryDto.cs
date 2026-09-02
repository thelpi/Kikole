namespace KikoleSite.Models.Dtos
{
    public class CountryDto : BaseDto
    {
        public required string Code { get; set; }

        public required string Name { get; set; }
    }
}
