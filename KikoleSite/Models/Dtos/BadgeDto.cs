namespace KikoleSite.Models.Dtos
{
    public class BadgeDto : BaseDto
    {
        public required string Name { get; set; }

        public required string Description { get; set; }

        public byte Hidden { get; set; }
    }
}
