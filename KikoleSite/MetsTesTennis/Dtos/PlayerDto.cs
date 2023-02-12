using System;

namespace KikoleSite.MetsTesTennis.Dtos
{
    public class PlayerDto
    {
        public ulong Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Hand { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public byte? Height { get; set; }
        public string WikidataId { get; set; }
    }
}
