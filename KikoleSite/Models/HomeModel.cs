using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api;
using KikoleSite.ItemDatas;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KikoleSite.Models
{
    public class HomeModel
    {
        public string Clue { get; set; }
        public string Message { get; set; }

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
                default: return null;
            }
        }

        internal void SetPropertiesFromProposal(ProposalResponse response,
            IReadOnlyDictionary<ulong, string> countries,
            IReadOnlyDictionary<ulong, string> positions)
        {
            Points = response.TotalPoints;
            switch (response.ProposalType)
            {
                case ProposalType.Club:
                    if (response.Successful)
                    {
                        var clubSubmissions = KnownPlayerClubs?.ToList() ?? new List<PlayerClub>();
                        if (!clubSubmissions.Any(cs => cs.Name == response.Value.name.ToString()))
                        {
                            clubSubmissions.Add(new PlayerClub
                            {
                                HistoryPosition = response.Value.historyPosition,
                                Name = response.Value.name.ToString()
                            });
                        }
                        KnownPlayerClubs = clubSubmissions.OrderBy(cs => cs.HistoryPosition).ToList();
                    }
                    else
                    {
                        var clValue = response.Value.ToString();
                        if (Helper.IsPropertyExist(response.Value, "name"))
                            clValue = response.Value.name;
                        IncorrectClubs = AddToList(IncorrectClubs, clValue);
                    }
                    break;
                case ProposalType.Country:
                    var cValue = response.Value.ToString();
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
                    var pValue = response.Value.ToString();
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
                    var nValue = response.Value.ToString();
                    if (response.Successful)
                        PlayerName = nValue;
                    else
                        IncorrectNames = AddToList(IncorrectNames, nValue);
                    break;
                case ProposalType.Year:
                    var yValue = response.Value.ToString();
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
