using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ClubProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Club;

        internal override ProposalResponse CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            var c = clubs.FirstOrDefault(_ => _.AllowedNames.Contains(Value.Sanitize()));

            return new ProposalResponse
            {
                Successful = c != null,
                Value = c != null
                    ? new PlayerClub(c, playerClubs.First(_ => _.ClubId == c.Id))
                    : null
            };
        }
    }
}
