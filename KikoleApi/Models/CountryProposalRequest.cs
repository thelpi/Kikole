using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Country;

        internal override ProposalResponse CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            bool? success = null;
            var value = ProposalResponse.GetValueFromProposalType(ProposalType,
                Value, ref success, player, playerClubs, clubs);

            return new ProposalResponse
            {
                Successful = success.Value,
                Value = value,
                TotalPoints = SourcePoints,
                LostPoints = success.Value
                    ? 0
                    : ProposalChart.Default.ProposalTypesCost[ProposalType],
                ProposalType = ProposalType
            };
        }

        internal override string IsValid()
        {
            if (int.TryParse(Value, out var countryId))
            {
                if (!Enum.GetValues(typeof(Country)).Cast<int>().Contains(countryId))
                    return "Invalid value";
            }
            else
            {
                if (!Enum.IsDefined(typeof(Country), Value))
                    return "Invalid value";
            }

            return null;
        }
    }
}
