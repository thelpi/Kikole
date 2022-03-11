using System;
using System.Linq;

namespace KikoleApi.Models.Requests
{
    public class PositionProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Position;

        internal override string IsValid()
        {
            if (int.TryParse(Value, out var positionId))
            {
                if (!Enum.GetValues(typeof(Position)).Cast<int>().Contains(positionId))
                    return "Invalid value";
            }
            else
            {
                if (!Enum.IsDefined(typeof(Position), Value))
                    return "Invalid value";
            }

            return null;
        }
    }
}
