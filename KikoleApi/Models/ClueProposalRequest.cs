using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ClueProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Clue;

        internal override ProposalResponse CheckSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            var pc = playerClubs.OrderBy(_ => _.ImportancePosition).First();
            var c = clubs.First(_ => _.Id == pc.ClubId);
            return new ProposalResponse
            {
                Successful = true,
                Value = new PlayerClub(c, pc)
            };
        }

        internal override string IsValid()
        {
            return null;
        }
    }
}
