namespace KikoleSite.Models.Dtos
{
    public class BadgeDto : BaseDto
    {
        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public byte Hidden { get; set; }
    }
}
