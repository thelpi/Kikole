namespace KikoleSite.Models.Dtos
{
    public class ClubDto : BaseDto
    {
        public string Name { get; set; } = null!;

        public string AllowedNames { get; set; } = null!;
    }
}
