using System;
using System.Linq;
using KikoleApi.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleApi.Models.Requests
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Country;

        internal override string IsValid(IStringLocalizer resources)
        {
            if (Value == null)
                return resources["InvalidValue"];

            if (int.TryParse(Value, out var countryId))
            {
                if (!Enum.GetValues(typeof(Countries)).Cast<int>().Contains(countryId))
                    return resources["InvalidValue"];
            }
            else
            {
                if (!Enum.IsDefined(typeof(Countries), Value))
                    return resources["InvalidValue"];
            }

            return null;
        }
    }
}
