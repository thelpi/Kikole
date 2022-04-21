using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KikoleSite.Models
{
    public class HomeModel
    {
        public bool CanCreateClub { get; set; }
        public string Clue { get; set; }
        public string EasyClue { get; set; }
        public string Message { get; set; }
        public string PlayerCreator { get; set; }

        public bool AlmostThere { get; set; }
        public IReadOnlyCollection<UserBadge> Badges { get; set; }
        public ProposalChart Chart { get; set; }
        public int Points { get; set; }
        public string MessageToDisplay { get; set; }
        public bool IsErrorMessage { get; set; }
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
        public IReadOnlyList<SelectListItem> Positions { get; set; }
        public string LoggedAs { get; set; }
        public int CurrentDay { get; set; }
        public bool NoPreviousDay { get; set; }
        public bool IsCreator { get; set; }
        public Challenge TodayChallenge { get; set; }
        public bool HasPendingChallenges { get; set; }

        public IReadOnlyList<string> IncorrectClubs { get; set; }
        public IReadOnlyList<string> IncorrectCountries { get; set; }
        public IReadOnlyList<string> IncorrectYears { get; set; }
        public IReadOnlyList<string> IncorrectPositions { get; set; }
        public IReadOnlyList<string> IncorrectNames { get; set; }

        public int NextDay => CurrentDay - 1;
        public int PreviousDay => CurrentDay + 1;

        public HomeModel() { }

        internal string GetValueFromProposalType(ProposalType proposalType)
        {
            switch (proposalType)
            {
                case ProposalType.Club: return ClubNameSubmission;
                case ProposalType.Country: return CountryNameSubmission;
                case ProposalType.Name: return PlayerNameSubmission;
                case ProposalType.Year: return BirthYearSubmission;
                case ProposalType.Position: return PositionSubmission;
                case ProposalType.Clue: return "GetClue"; // anything not empty
                default: return null;
            }
        }

        internal void SetFinalFormIsUserIsCreator(string playerName)
        {
            PlayerName = playerName;
            IsCreator = true;
        }

        internal void SetPropertiesFromProposal(ProposalResponse response,
            IReadOnlyDictionary<ulong, string> countries,
            IReadOnlyDictionary<ulong, string> positions,
            string easyClue)
        {
            Points = response.TotalPoints;
            switch (response.ProposalType)
            {
                case ProposalType.Clue:
                    EasyClue = easyClue;
                    break;
                case ProposalType.Club:
                    if (response.Successful)
                    {
                        var clubSubmissions = KnownPlayerClubs?.ToList() ?? new List<PlayerClub>();
                        var newClubs = response.GetPlayerClubsValue();
                        clubSubmissions.AddRange(
                            newClubs.Where(nc =>
                                !clubSubmissions.Any(cs => cs.HistoryPosition == nc.HistoryPosition)));
                        KnownPlayerClubs = clubSubmissions.OrderBy(cs => cs.HistoryPosition).ToList();
                    }
                    else
                    {
                        IncorrectClubs = AddToList(IncorrectClubs, response.Value);
                    }
                    break;
                case ProposalType.Country:
                    var cValue = response.Value;
                    if (response.Successful)
                        CountryName = countries[ulong.Parse(cValue)];
                    else
                    {
                        if (ulong.TryParse(cValue, out ulong cId)
                            && countries.ContainsKey(cId))
                            cValue = countries[cId];
                        IncorrectCountries = AddToList(IncorrectCountries, cValue);
                    }
                    break;
                case ProposalType.Position:
                    var pValue = response.Value;
                    if (response.Successful)
                        Position = positions[ulong.Parse(pValue)];
                    else
                    {
                        if (ulong.TryParse(pValue, out ulong pId)
                            && positions.ContainsKey(pId))
                            pValue = positions[pId];
                        IncorrectPositions = AddToList(IncorrectPositions, pValue);
                    }
                    break;
                case ProposalType.Name:
                    var nValue = response.Value;
                    if (response.Successful)
                        PlayerName = nValue;
                    else
                        IncorrectNames = AddToList(IncorrectNames, nValue);
                    break;
                case ProposalType.Year:
                    var yValue = response.Value;
                    if (response.Successful)
                        BirthYear = yValue;
                    else
                        IncorrectYears = AddToList(IncorrectYears, yValue);
                    break;
            }
        }

        private IReadOnlyList<string> AddToList(IReadOnlyList<string> baseList, string value)
        {
            var list = (baseList ?? new List<string>(1)).ToList();
            list.Add(value);
            return list;
        }
    }
}
