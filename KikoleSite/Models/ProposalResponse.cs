﻿using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Helpers;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models
{
    public class ProposalResponse
    {
        private readonly List<UserBadge> _badges = new List<UserBadge>();

        public bool Successful { get; }

        public object Value { get; }

        public string RawValue { get; }

        public DateTime Date { get; }

        public string Tip { get; }

        public (int, bool) LostPoints { get; }

        public ProposalTypes ProposalType { get; }

        public int TotalPoints { get; private set; }

        public IReadOnlyCollection<UserBadge> CollectedBadges => _badges;

        internal bool IsWin => ProposalType == ProposalTypes.Name && Successful;

        private ProposalResponse(ProposalTypes proposalType,
            string sourceValue,
            bool? success,
            PlayerFullDto player)
        {
            ProposalType = proposalType;

            if (success.HasValue)
                Successful = success.Value;

            RawValue = sourceValue;

            switch (ProposalType)
            {
                case ProposalTypes.Name:
                    if (!success.HasValue)
                        Successful = player.Player.AllowedNames.ContainsApproximately(sourceValue);
                    Value = Successful
                        ? player.Player.Name
                        : sourceValue;
                    break;

                case ProposalTypes.Club:
                    var c = player.Clubs.FirstOrDefault(_ => _.AllowedNames.ContainsSanitized(sourceValue));
                    if (!success.HasValue)
                        Successful = c != null;
                    if (Successful)
                    {
                        if (c == null)
                        {
                            // weird case from the beginning when there was no autocompletion on clubs
                            // the value is not really used in that case anyway
                            // it just need to be not null
                            Value = new List<PlayerClub>();
                        }
                        else
                        {
                            Value = player.PlayerClubs
                                .Where(_ => _.ClubId == c.Id)
                                .Select(_ => new PlayerClub(_, player.Clubs))
                                .ToList();
                        }
                    }
                    else
                        Value = sourceValue;
                    break;

                case ProposalTypes.Country:
                    if (!success.HasValue)
                        Successful = player.Player.CountryId == (ulong)Enum.Parse<Countries>(sourceValue);
                    Value = Successful
                        ? player.Player.CountryId
                        : (object)sourceValue;
                    RawValue = Enum.TryParse<Countries>(sourceValue, out var tmpRawCountry)
                        ? tmpRawCountry.ToString()
                        : RawValue;
                    break;

                case ProposalTypes.Continent:
                    if (!success.HasValue)
                        Successful = player.Player.ContinentId == (ulong)Enum.Parse<Continents>(sourceValue);
                    Value = Successful
                        ? player.Player.ContinentId
                        : (object)sourceValue;
                    RawValue = Enum.TryParse<Continents>(sourceValue, out var tmpRawContinent)
                        ? tmpRawContinent.ToString()
                        : RawValue;
                    break;

                case ProposalTypes.Position:
                    if (!success.HasValue)
                        Successful = player.Player.PositionId == ulong.Parse(sourceValue);
                    Value = Successful
                        ? player.Player.PositionId
                        : (object)sourceValue;
                    RawValue = Enum.TryParse<Positions>(sourceValue, out var tmpRawPosition)
                        ? tmpRawPosition.ToString()
                        : RawValue;
                    break;

                case ProposalTypes.Year:
                    if (!success.HasValue)
                        Successful = ushort.Parse(sourceValue) == player.Player.YearOfBirth;
                    Value = Successful
                        ? player.Player.YearOfBirth
                        : (object)sourceValue;
                    break;

                case ProposalTypes.Leaderboard:
                case ProposalTypes.Clue:
                    if (!success.HasValue)
                        Successful = true;
                    Value = null;
                    RawValue = string.Empty;
                    break;
            }

            if (Successful && ProposalType.CanBeMiss())
                LostPoints = (0, false);
            else
                LostPoints = ProposalChart.ProposalTypesCost[ProposalType];
        }

        internal ProposalResponse(ProposalRequest request, PlayerFullDto player, IStringLocalizer resources)
            : this(request.ProposalType, request.Value, null, player)
        {
            Date = request.ProposalDateTime;
            Tip = request.GetTip(player.Player, resources);
        }

        internal ProposalResponse(ProposalDto dto, PlayerFullDto player, IStringLocalizer resources)
            : this((ProposalTypes)dto.ProposalTypeId, dto.Value, dto.Successful > 0, player)
        {
            // a bit ugly, ngl
            if ((ProposalTypes)dto.ProposalTypeId == ProposalTypes.Year)
            {
                Tip = Convert.ToUInt16(Value) > player.Player.YearOfBirth
                    ? resources["TipOlderPlayerShort"]
                    : resources["TipYoungerPlayerShort"];
            }
            Date = dto.CreationDate;
        }

        internal ProposalResponse WithTotalPoints(int sourcePoints, bool duplicate)
        {
            var lostPoints = 0;
            if (!duplicate)
            {
                lostPoints = LostPoints.Item2
                    ? (int)Math.Round(sourcePoints * LostPoints.Item1 / (decimal)100)
                    : LostPoints.Item1;
            }
            TotalPoints = Math.Max(0, sourcePoints - lostPoints);
            return this;
        }

        internal void AddBadge(UserBadge badge)
        {
            _badges.Add(badge);
        }
    }
}
