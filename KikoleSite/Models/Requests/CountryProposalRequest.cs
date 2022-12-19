using System;
using System.Linq;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Country;
    }
}
