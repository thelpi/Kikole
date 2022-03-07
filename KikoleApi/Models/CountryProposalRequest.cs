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
            return new ProposalResponse
            {
                Successful = player.CountryId == (ulong)Enum.Parse<Country>(Value),
                Value = ((Country)player.CountryId).ToString()
            };
        }

        internal override string IsValid()
        {
            if (!Enum.IsDefined(typeof(Country), Value))
                return "Invalid value";

            return null;
        }
    }
}
