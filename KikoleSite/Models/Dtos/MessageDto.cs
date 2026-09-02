using System;

namespace KikoleSite.Models.Dtos
{
    public class MessageDto : BaseDto
    {
        public string Message { get; set; } = null!;

        public DateTime? DisplayFrom { get; set; }

        public DateTime? DisplayTo { get; set; }
    }
}
