using System;
using System.Linq;

namespace KikoleApi.Models.Requests
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Country;

        internal override string IsValid()
        {
            if (Value == null)
                return "Invalid value";

            if (int.TryParse(Value, out var countryId))
            {
                if (!Enum.GetValues(typeof(Countries)).Cast<int>().Contains(countryId))
                    return "Invalid value";
            }
            else
            {
                if (!Enum.IsDefined(typeof(Countries), Value))
                    return "Invalid value";
            }

            return null;
        }
    }
}
