using System.Collections.Generic;
using KikoleSite.Api;

namespace KikoleSite.Models
{
    public class PlayerSubmissionModel
    {
        public string Name { get; set; }

        public string Login { get; set; }

        public ulong Id { get; set; }

        public IReadOnlyCollection<PlayerClub> Clubs { get; set; }

        public string AllowedNames { get; set; }

        public ushort YearOfBirth { get; set; }

        public string Country { get; set; }

        public string Clue { get; set; }

        public string EasyClue { get; set; }

        public string Position { get; set; }
    }
}
