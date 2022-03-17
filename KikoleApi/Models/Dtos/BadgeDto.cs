namespace KikoleApi.Models.Dtos
{
    public class BadgeDto : BaseDto
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public ulong Users { get; set; }
    }
}
