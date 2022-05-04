﻿using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Helpers;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Api.Models.Requests
{
    public class PlayerRequest
    {
        public string Name { get; set; }

        public ushort YearOfBirth { get; set; }

        internal Countries Country => Enum.GetValues(typeof(Countries)).Cast<Countries>().Single(_ => (ulong)_ == ulong.Parse(CountryString));

        public string CountryString { get; set; }

        public DateTime? ProposalDate { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }

        public IReadOnlyList<PlayerClubRequest> Clubs { get; set; }

        public string ClueEn { get; set; }

        public string EasyClueEn { get; set; }

        public IReadOnlyDictionary<string, string> ClueLanguages { get; set; }

        public IReadOnlyDictionary<string, string> EasyClueLanguages { get; set; }

        internal Positions Position => Enum.Parse<Positions>(PositionString);

        public string PositionString { get; set; }

        public bool SetLatestProposalDate { get; set; }

        public bool HideCreator { get; set; }

        internal string IsValid(DateTime today, IStringLocalizer resources)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return resources["InvalidName"];

            var countries = Enum.GetValues(typeof(Countries)).Cast<Countries>();
            if (!ulong.TryParse(CountryString, out var countryId) || !countries.Any(_ => (ulong)_ == countryId))
                return resources["InvalidCountry"];

            if (!Enum.TryParse<Positions>(PositionString, out _))
                return resources["InvalidPosition"];

            if (YearOfBirth < 1850 || YearOfBirth > 2100)
                return resources["InvalidBirthYear"];

            if (!AllowedNames.IsValid())
                return resources["InvalidAllowedNames"];

            if (Clubs == null || Clubs.Count == 0)
                return resources["EmptyClubsList"];

            if (Clubs.Any(c => c.ClubId == 0))
                return resources["InvalidClubs"];

            var historyCheck = Clubs.Select(c => c.HistoryPosition);
            if (historyCheck.Distinct().Count() != Clubs.Count
                || historyCheck.Min() != 1
                || historyCheck.Max() - Clubs.Count != 0)
                return resources["InvalidClubs"];

            if (string.IsNullOrWhiteSpace(ClueEn) || string.IsNullOrWhiteSpace(EasyClueEn))
                return resources["InvalidClue"];

            if (ProposalDate.HasValue && ProposalDate.Value.Date < today)
                return resources["InvalidProposalDate"];

            return null;
        }

        internal PlayerDto ToDto(ulong userId)
        {
            return new PlayerDto
            {
                CountryId = (ulong)Country,
                Name = Name,
                ProposalDate = ProposalDate,
                YearOfBirth = YearOfBirth,
                AllowedNames = AllowedNames.SanitizeJoin(Name),
                Clue = ClueEn,
                EasyClue = EasyClueEn,
                PositionId = (ulong)Position,
                CreationUserId = userId,
                HideCreator = (byte)(HideCreator ? 1 : 0)
            };
        }

        internal IReadOnlyList<PlayerClubDto> ToPlayerClubDtos(ulong playerId)
        {
            return Clubs
                .Select(c => c.ToPlayerClubDto(playerId))
                .ToList();
        }
    }
}
