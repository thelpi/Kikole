using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Requests;

namespace KikoleApi.Models
{
    public class ProposalResponse
    {
        public bool Successful { get; set; }

        public object Value { get; set; }

        public string Tip { get; set; }

        public int LostPoints { get; set; }

        public int TotalPoints { get; private set; }

        public ProposalTypes ProposalType { get; set; }

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
                        : null;
                    break;

                case ProposalTypes.Club:
                    var c = clubs.FirstOrDefault(_ => _.AllowedNames.ContainsSanitized(sourceValue));
                    if (!success.HasValue)
                        Successful = c != null;
                    Value = Successful
                        ? new PlayerClub(c, playerClubs.First(_ => _.ClubId == c.Id))
                        : null;
                    break;

                case ProposalTypes.Country:
                    if (!success.HasValue)
                        Successful = player.CountryId == (ulong)Enum.Parse<Countries>(sourceValue);
                    Value = Successful
                        ? player.CountryId
                        : default(ulong?);
                    break;

                case ProposalTypes.Position:
                    if (!success.HasValue)
                        Successful = player.PositionId == ulong.Parse(sourceValue);
                    Value = Successful
                        ? player.PositionId
                        : default(ulong?);
                    break;

                case ProposalTypes.Year:
                    if (!success.HasValue)
                        Successful = ushort.Parse(sourceValue) == player.YearOfBirth;
                    Value = Successful
                        ? player.YearOfBirth.ToString()
                        : null;
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

        internal ProposalResponse WithTotalPoints(int sourcePoints)
        {
            TotalPoints = Math.Max(0, sourcePoints - LostPoints);
            return this;
        }
    }
}
