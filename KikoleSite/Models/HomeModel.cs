using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Models;
using KikoleSite.Api.Models.Enums;
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
        public bool IsErrorMessageForced { get; set; }
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
        public IReadOnlyList<(string, string)> IncorrectYears { get; set; }
        public IReadOnlyList<string> IncorrectPositions { get; set; }
        public IReadOnlyList<string> IncorrectNames { get; set; }

        public int NextDay => CurrentDay - 1;
        public int PreviousDay => CurrentDay + 1;

        public HomeModel() { }

        internal string GetValueFromProposalType(ProposalTypes proposalType)
        {
            return proposalType switch
            {
                ProposalTypes.Club => ClubNameSubmission,
                ProposalTypes.Country => CountryNameSubmission,
                ProposalTypes.Name => PlayerNameSubmission,
                ProposalTypes.Year => BirthYearSubmission,
                ProposalTypes.Position => PositionSubmission,
                ProposalTypes.Clue => "GetClue",// anything not empty
                _ => null,
            };
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
                case ProposalTypes.Clue:
                    EasyClue = easyClue;
                    break;
                case ProposalTypes.Club:
                    if (response.Successful)
                    {
                        var clubSubmissions = KnownPlayerClubs?.ToList() ?? new List<PlayerClub>();
                        var newClubs = response.Value as IReadOnlyCollection<PlayerClub>;
                        clubSubmissions.AddRange(
                            newClubs.Where(nc =>
                                !clubSubmissions.Any(cs => cs.HistoryPosition == nc.HistoryPosition)));
                        KnownPlayerClubs = clubSubmissions.OrderBy(cs => cs.HistoryPosition).ToList();
                    }
                    else
                    {
                        IncorrectClubs = AddToList(IncorrectClubs, response.Value.ToString());
                    }
                    break;
                case ProposalTypes.Country:
                    var cValue = response.Value.ToString();
                    if (response.Successful)
                        CountryName = countries[ulong.Parse(cValue)];
                    else
                    {
                        if (ulong.TryParse(cValue, out ulong cId) && countries.ContainsKey(cId))
                            cValue = countries[cId];
                        IncorrectCountries = AddToList(IncorrectCountries, cValue);
                    }
                    break;
                case ProposalTypes.Position:
                    var pValue = response.Value.ToString();
                    if (response.Successful)
                        Position = positions[Convert.ToUInt16(pValue)];
                    else
                    {
                        if (ulong.TryParse(pValue, out ulong pId) && positions.ContainsKey(pId))
                            pValue = positions[pId];
                        IncorrectPositions = AddToList(IncorrectPositions, pValue);
                    }
                    break;
                case ProposalTypes.Name:
                    var nValue = response.Value;
                    if (response.Successful)
                        PlayerName = nValue.ToString();
                    else
                        IncorrectNames = AddToList(IncorrectNames, nValue.ToString());
                    break;
                case ProposalTypes.Year:
                    var yValue = response.Value.ToString();
                    if (response.Successful)
                        BirthYear = yValue;
                    else
                        IncorrectYears = AddToList(IncorrectYears, (yValue, response.Tip));
                    break;
            }
        }

        private IReadOnlyList<T> AddToList<T>(IReadOnlyList<T> baseList, T value)
        {
            var list = (baseList ?? new List<T>(1)).ToList();
            list.Add(value);
            return list;
        }
    }
}
