using System;
using System.Collections.Generic;
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
            var countryId = (ulong)Enum.Parse<Country>(Value);

            var pr = new ProposalResponse
            {
                Successful = true
            };

            if (player.Country1Id == countryId)
                pr.Value = ((Country)player.Country1Id).ToString();
            else if (player.Country2Id == countryId)
                pr.Value = ((Country)player.Country2Id).ToString();
            else
                pr.Successful = false;

            return pr;
        }

        internal override string IsValid()
        {
            if (!Enum.IsDefined(typeof(Country), Value))
                return "Invalid value";

            return null;
        }
    }
}
