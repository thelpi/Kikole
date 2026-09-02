using System;

namespace KikoleSite.Models.Dtos
{
    public class MessageDto : BaseDto
    {
        public required string Message { get; set; }

        public DateTime? DisplayFrom { get; set; }

        public DateTime? DisplayTo { get; set; }
    }
}
