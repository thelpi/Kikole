namespace KikoleSite.Models.Dtos
{
    public class DiscussionDto : BaseDto
    {
        public ulong UserId { get; set; }

        public required string Email { get; set; }

        public required string Message { get; set; }
    }
}
