using System.Collections.Generic;
using KikoleSite.ItemDatas;

namespace KikoleSite.Cookies
{
    public class SubmissionForm
    {
        public int Points { get; set; }
        public string Clue { get; set; }
        public string BirthYear { get; set; }
        public string PlayerName { get; set; }
        public string CountryName { get; set; }
        public IReadOnlyList<PlayerClub> KnownPlayerClubs { get; set; }
        public int CurrentDay { get; set; }
        public bool NoPreviousDay { get; set; }
        public string Position { get; set; }
    }
}
