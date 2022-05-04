using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Helpers;
using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;
using KikoleSite.Api.Models.Requests;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Api.Models
{
    public class ProposalResponse
    {
        private readonly List<UserBadge> _badges = new List<UserBadge>();

        public bool Successful { get; }

        public string Value { get; }

        public string Tip { get; }

        public int LostPoints { get; }

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

            switch (ProposalType)
            {
                case ProposalTypes.Name:
                    if (!success.HasValue)
                        Successful = player.Player.AllowedNames.ContainsSanitized(sourceValue);
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
                        Value = System.Text.Json.JsonSerializer.Serialize(
                            player.PlayerClubs
                                .Where(_ => _.ClubId == c.Id)
                                .Select(_ => new PlayerClub(_, player.Clubs))
                                .ToList());
                    }
                    else
                        Value = sourceValue;
                    break;

                case ProposalTypes.Country:
                    if (!success.HasValue)
                        Successful = player.Player.CountryId == (ulong)Enum.Parse<Countries>(sourceValue);
                    Value = Successful
                        ? player.Player.CountryId.ToString()
                        : sourceValue;
                    break;

                case ProposalTypes.Position:
                    if (!success.HasValue)
                        Successful = player.Player.PositionId == ulong.Parse(sourceValue);
                    Value = Successful
                        ? player.Player.PositionId.ToString()
                        : sourceValue;
                    break;

                case ProposalTypes.Year:
                    if (!success.HasValue)
                        Successful = ushort.Parse(sourceValue) == player.Player.YearOfBirth;
                    Value = Successful
                        ? player.Player.YearOfBirth.ToString()
                        : sourceValue;
                    break;

                case ProposalTypes.Clue:
                    if (!success.HasValue)
                        Successful = true;
                    Value = null;
                    break;
            }
            
            LostPoints = Successful && ProposalType != ProposalTypes.Clue
                ? 0
                : ProposalChart.Default.ProposalTypesCost[ProposalType];
        }

        internal ProposalResponse(BaseProposalRequest request, PlayerFullDto player, IStringLocalizer resources)
            : this(request.ProposalType, request.Value, null, player)
        {
            Tip = request.GetTip(player.Player, resources);
        }

        internal ProposalResponse(ProposalDto dto, PlayerFullDto player, IStringLocalizer resources)
            : this((ProposalTypes)dto.ProposalTypeId, dto.Value, dto.Successful > 0, player)
        {
            // a bit ugly, ngl
            if ((ProposalTypes)dto.ProposalTypeId == ProposalTypes.Year)
            {
                Tip = ushort.Parse(Value) > player.Player.YearOfBirth
                    ? resources["TipOlderPlayerShort"]
                    : resources["TipYoungerPlayerShort"];
            }
        }

        internal ProposalResponse WithTotalPoints(int sourcePoints, bool duplicate)
        {
            TotalPoints = Math.Max(0, sourcePoints - (duplicate ? 0 : LostPoints));
            return this;
        }

        internal void AddBadge(UserBadge badge)
        {
            _badges.Add(badge);
        }
    }
}
