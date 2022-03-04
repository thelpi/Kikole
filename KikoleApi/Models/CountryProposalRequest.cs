using System;
using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Country;

        internal override string IsSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            var countryId = (ulong)Enum.Parse<Country>(Value);
            var ok1 = player.Country1Id == countryId;
            var ok2 = player.Country2Id == countryId;
            return ok1
                ? ((Country)player.Country1Id).ToString()
                : (ok2
                    ? ((Country)player.Country2Id).ToString()
                    : null);
        }

        internal override string IsValid()
        {
            if (!Enum.IsDefined(typeof(Country), Value))
                return "Invalid value";

            return null;
        }
    }
}
