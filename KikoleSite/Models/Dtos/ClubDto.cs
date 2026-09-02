namespace KikoleSite.Models.Dtos
{
    public class ClubDto : BaseDto
    {
        public required string Name { get; set; }

        public required string AllowedNames { get; set; }
    }
}
