using System.Collections.Generic;
using KikoleSite.Api;
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
        public string Position { get; set; }
        public IReadOnlyList<PlayerClub> KnownPlayerClubs { get; set; }
        public string ClubNameSubmission { get; set; }
        public string PlayerNameSubmission { get; set; }
        public string CountryNameSubmission { get; set; }
        public string BirthYearSubmission { get; set; }
        public string PositionSubmission { get; set; }
        public IReadOnlyDictionary<ulong, string> Countries { get; set; }
        public IReadOnlyDictionary<ulong, string> Positions { get; set; }
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
            Position = submissionFormCookie.Position;
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
                Points = Points,
                Position = Position
            };
        }

        internal string GetValueFromProposalType(ProposalType proposalType)
        {
            switch (proposalType)
            {
                case ProposalType.Club: return ClubNameSubmission;
                case ProposalType.Country: return CountryNameSubmission;
                case ProposalType.Name: return PlayerNameSubmission;
                case ProposalType.Year: return BirthYearSubmission;
                case ProposalType.Position: return PositionSubmission;
                default: return null;
            }
        }
    }
}
