using System;
using System.Linq;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Country;

        internal override string IsValid()
        {
            if (Value == null)
                return SPA.TextResources.InvalidValue;

            if (int.TryParse(Value, out var countryId))
            {
                if (!Enum.GetValues(typeof(Countries)).Cast<int>().Contains(countryId))
                    return SPA.TextResources.InvalidValue;
            }
            else
            {
                if (!Enum.IsDefined(typeof(Countries), Value))
                    return SPA.TextResources.InvalidValue;
            }

            return null;
        }
    }
}
