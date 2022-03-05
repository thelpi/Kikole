using System;
using System.Collections.Generic;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Country;

        internal override void CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            var countryId = (ulong)Enum.Parse<Country>(Value);

            Successful = true;
            if (player.Country1Id == countryId)
                SuccessfulValue = ((Country)player.Country1Id).ToString();
            else if (player.Country2Id == countryId)
                SuccessfulValue = ((Country)player.Country2Id).ToString();
            else
                Successful = false;
        }

        internal override string IsValid()
        {
            if (!Enum.IsDefined(typeof(Country), Value))
                return "Invalid value";

            return null;
        }
    }
}
