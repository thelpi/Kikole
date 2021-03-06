using System;

namespace KikoleSite.Api.Models.Dtos
{
    public abstract class BaseDto
    {
        public ulong Id { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime UpdateDate { get; set; }
    }
}
