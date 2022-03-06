using System.Collections.Generic;

namespace KikoleSite.Models
{
    public class MainModel
    {
        public int Points { get; set; }
        public string MessageToDisplay { get; set; }
        public bool IsErrorMessage { get; set; }

        public string BirthYear { get; set; }
        public string PlayerName { get; set; }
        public string CountryName { get; set; }
        public IReadOnlyList<PlayerClub> KnownPlayerClubs { get; set; }

        public string ClubNameSubmission { get; set; }
        public string PlayerNameSubmission { get; set; }
        public string CountryNameSubmission { get; set; }
        public string BirthYearSubmission { get; set; }

        internal MainModel ClearNonPersistentData()
        {
            ClubNameSubmission = null;
            CountryNameSubmission = null;
            IsErrorMessage = false;
            MessageToDisplay = null;
            PlayerNameSubmission = null;
            BirthYearSubmission = null;
            return this;
        }

        internal void RemovePoints(int ptsToRemove)
        {
            Points -= ptsToRemove;
            Points = Points < 0 ? 0 : Points;
        }
    }
}
