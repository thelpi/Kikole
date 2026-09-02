namespace KikoleSite.Models.Dtos
{
    public class DiscussionDto : BaseDto
    {
        public ulong UserId { get; set; }

        public string Email { get; set; } = null!;

        public string Message { get; set; } = null!;
    }
}
