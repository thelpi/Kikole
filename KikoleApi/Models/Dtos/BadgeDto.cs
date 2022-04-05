namespace KikoleApi.Models.Dtos
{
    public class BadgeDto : BaseDto
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public byte Hidden { get; set; }

        public byte IsUnique { get; set; }
    }
}
