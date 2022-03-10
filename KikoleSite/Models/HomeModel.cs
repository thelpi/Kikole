using System.Collections.Generic;
using KikoleSite.Cookies;
using KikoleSite.ItemDatas;

namespace KikoleSite.Models
{
    public class HomeModel
    {
        public int Points { get; set; }
        public string MessageToDisplay { get; set; }
        public bool IsErrorMessage { get; set; }
        public string Clue { get; set; }
        public string BirthYear { get; set; }
        public string PlayerName { get; set; }
        public string CountryName { get; set; }
        public IReadOnlyList<PlayerClub> KnownPlayerClubs { get; set; }
        public string ClubNameSubmission { get; set; }
        public string PlayerNameSubmission { get; set; }
        public string CountryNameSubmission { get; set; }
        public string BirthYearSubmission { get; set; }
        public IReadOnlyDictionary<ulong, string> Countries { get; set; }
        public string LoggedAs { get; set; }
        public int CurrentDay { get; set; }
        public bool NoPreviousDay { get; set; }

        public int NextDay => CurrentDay - 1;
        public int PreviousDay => CurrentDay + 1;

        public HomeModel() { }

        internal HomeModel(SubmissionForm submissionFormCookie)
        {
            BirthYear = submissionFormCookie.BirthYear;
            Clue = submissionFormCookie.Clue;
            CountryName = submissionFormCookie.CountryName;
            CurrentDay = submissionFormCookie.CurrentDay;
            KnownPlayerClubs = submissionFormCookie.KnownPlayerClubs;
            NoPreviousDay = submissionFormCookie.NoPreviousDay;
            PlayerName = submissionFormCookie.PlayerName;
            Points = submissionFormCookie.Points;
        }

        internal SubmissionForm ToSubmissionFormCookie()
        {
            return new SubmissionForm
            {
                BirthYear = BirthYear,
                Clue = Clue,
                CountryName = CountryName,
                CurrentDay = CurrentDay,
                KnownPlayerClubs = KnownPlayerClubs,
                NoPreviousDay = NoPreviousDay,
                PlayerName = PlayerName,
                Points = Points
            };
        }

        internal void RemovePoints(int ptsToRemove)
        {
            Points -= ptsToRemove;
            Points = Points < 0 ? 0 : Points;
        }
    }
}
