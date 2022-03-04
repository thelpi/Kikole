using System;
using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Country;

        internal override bool IsSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            var countryId = (ulong)Enum.Parse<Country>(Value);
            return player.Country1Id == countryId
                || player.Country2Id == countryId;
        }

        internal override string IsValid()
        {
            if (!Enum.IsDefined(typeof(Country), Value))
                return "Invalid value";

            return null;
        }
    }
}
