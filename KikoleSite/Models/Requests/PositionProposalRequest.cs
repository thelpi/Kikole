using System;
using System.Linq;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests
{
    public class PositionProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Position;
    }
}
