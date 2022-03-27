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
            PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            ProposalType = proposalType;

            if (success.HasValue)
                Successful = success.Value;

            switch (proposalType)
            {
                case ProposalTypes.Name:
                    if (!success.HasValue)
                        Successful = player.AllowedNames.ContainsSanitized(sourceValue);
                    Value = Successful
                        ? player.Name
                        : sourceValue;
                    break;

                case ProposalTypes.Club:
                    var c = clubs.FirstOrDefault(_ => _.AllowedNames.ContainsSanitized(sourceValue));
                    if (!success.HasValue)
                        Successful = c != null;
                    Value = Successful
                        ? new PlayerClub(c, playerClubs)
                        : (object)sourceValue;
                    break;

                case ProposalTypes.Country:
                    if (!success.HasValue)
                        Successful = player.CountryId == (ulong)Enum.Parse<Countries>(sourceValue);
                    Value = Successful
                        ? player.CountryId
                        : (object)sourceValue;
                    break;

                case ProposalTypes.Position:
                    if (!success.HasValue)
                        Successful = player.PositionId == ulong.Parse(sourceValue);
                    Value = Successful
                        ? player.PositionId
                        : (object)sourceValue;
                    break;

                case ProposalTypes.Year:
                    if (!success.HasValue)
                        Successful = ushort.Parse(sourceValue) == player.YearOfBirth;
                    Value = Successful
                        ? player.YearOfBirth.ToString()
                        : sourceValue;
                    break;
            }
            
            LostPoints = Successful
                ? 0
                : ProposalChart.Default.ProposalTypesCost[ProposalType];
        }

        internal ProposalResponse(BaseProposalRequest request,
            PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
            : this(request.ProposalType, request.Value, null, player, playerClubs, clubs)
        {
            Tip = request.GetTip(player);
        }

        internal ProposalResponse(ProposalDto dto,
            PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
            : this((ProposalTypes)dto.ProposalTypeId, dto.Value, dto.Successful > 0, player, playerClubs, clubs)
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
