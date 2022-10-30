using System;
using System.Linq;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests
{
    public class ContinentProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Continent;

        internal override string IsValid(IStringLocalizer resources)
        {
            if (Value == null)
                return resources["InvalidValue"];

            if (int.TryParse(Value, out var continentId))
            {
                if (!Enum.GetValues(typeof(Continents)).Cast<int>().Contains(continentId))
                    return resources["InvalidValue"];
            }
            else
            {
                if (!Enum.IsDefined(typeof(Continents), Value))
                    return resources["InvalidValue"];
            }

            return null;
        }
    }
}
