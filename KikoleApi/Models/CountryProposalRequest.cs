using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Country;

        internal override int PointsCost => 25;

        internal override ProposalResponse CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            var success = player.CountryId == (ulong)Enum.Parse<Country>(Value);
            return new ProposalResponse
            {
                Successful = success,
                Value = player.CountryId,
                TotalPoints = SourcePoints,
                LostPoints = success ? 0 : PointsCost
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
