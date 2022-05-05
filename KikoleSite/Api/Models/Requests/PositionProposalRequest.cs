using System;
using System.Linq;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Api.Models.Requests
{
    public class PositionProposalRequest : BaseProposalRequest
    {
        public PositionProposalRequest(IClock clock)
            : base(clock) { }

        internal override ProposalTypes ProposalType => ProposalTypes.Position;

        internal override string IsValid(IStringLocalizer resources)
        {
            if (Value == null)
                return resources["InvalidValue"];

            if (int.TryParse(Value, out var positionId))
            {
                if (!Enum.GetValues(typeof(Positions)).Cast<int>().Contains(positionId))
                    return resources["InvalidValue"];
            }
            else
            {
                if (!Enum.IsDefined(typeof(Positions), Value))
                    return resources["InvalidValue"];
            }

            return null;
        }
    }
}
