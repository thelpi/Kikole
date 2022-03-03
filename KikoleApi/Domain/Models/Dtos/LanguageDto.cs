using System;

namespace KikoleApi.Domain.Models.Dtos
{
    internal class LanguageDto
    {
        public long Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime UpdateDate { get; set; }
    }
}
