using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;

namespace KikoleApi.Models
{
    public class ProposalResponse
    {
        private readonly List<UserBadge> _badges = new List<UserBadge>();

        public bool Successful { get; }

        public object Value { get; }

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
                    Value = Successful
                        ? new PlayerClub(c, player.PlayerClubs)
                        : (object)sourceValue;
                    break;

                case ProposalTypes.Country:
                    if (!success.HasValue)
                        Successful = player.Player.CountryId == (ulong)Enum.Parse<Countries>(sourceValue);
                    Value = Successful
                        ? player.Player.CountryId
                        : (object)sourceValue;
                    break;

                case ProposalTypes.Position:
                    if (!success.HasValue)
                        Successful = player.Player.PositionId == ulong.Parse(sourceValue);
                    Value = Successful
                        ? player.Player.PositionId
                        : (object)sourceValue;
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

        internal ProposalResponse(BaseProposalRequest request, PlayerFullDto player, TextResources resources)
            : this(request.ProposalType, request.Value, null, player)
        {
            Tip = request.GetTip(player.Player, resources);
        }

        internal ProposalResponse(ProposalDto dto, PlayerFullDto player)
            : this((ProposalTypes)dto.ProposalTypeId, dto.Value, dto.Successful > 0, player)
        { }

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
