using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ClueProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Clue;

        internal override string IsSuccessful(PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            return clubs.First(c => c.Id == playerClubs.OrderBy(pc => pc.ImportancePosition).First().ClubId).Name;
        }

        internal override string IsValid()
        {
            return null;
        }
    }
}
