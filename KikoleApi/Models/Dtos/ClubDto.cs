using System;

namespace KikoleApi.Models.Dtos
{
    public class ClubDto
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public string AllowedNames { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime UpdateDate { get; set; }
    }
}
