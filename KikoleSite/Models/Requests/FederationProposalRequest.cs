using System;
using System.Linq;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests
{
    public class FederationProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Federation;

        internal override string IsValid(IStringLocalizer resources)
        {
            if (Value == null)
                return resources["InvalidValue"];

            if (int.TryParse(Value, out var federationId))
            {
                if (!Enum.GetValues(typeof(Federations)).Cast<int>().Contains(federationId))
                    return resources["InvalidValue"];
            }
            else
            {
                if (!Enum.IsDefined(typeof(Federations), Value))
                    return resources["InvalidValue"];
            }

            return null;
        }
    }
}
