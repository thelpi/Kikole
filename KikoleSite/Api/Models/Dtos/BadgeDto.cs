namespace KikoleSite.Api.Models.Dtos
{
    public class BadgeDto : BaseDto
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public byte Hidden { get; set; }

        public byte Retroactive { get; set; }
    }
}
