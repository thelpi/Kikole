using System;

namespace KikoleSite.Api.Models.Dtos
{
    public class MessageDto : BaseDto
    {
        public string Message { get; set; }

        public DateTime? DisplayFrom { get; set; }

        public DateTime? DisplayTo { get; set; }
    }
}
