using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models
{
    public class Continent
    {
        public Continents Id { get; }

        public string Name { get; }

        internal Continent(ContinentDto dto)
        {
            Id = (Continents)dto.Id;
            Name = dto.Name;
        }
    }
}
