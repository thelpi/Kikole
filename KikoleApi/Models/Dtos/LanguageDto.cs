using System;

namespace KikoleApi.Models.Dtos
{
    internal class LanguageDto
    {
        public ulong Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime UpdateDate { get; set; }
    }
}
