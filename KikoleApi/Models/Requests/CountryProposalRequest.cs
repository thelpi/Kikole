﻿using System;
using System.Linq;

namespace KikoleApi.Models.Requests
{
    public class CountryProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Country;

        internal override string IsValid()
        {
            if (int.TryParse(Value, out var countryId))
            {
                if (!Enum.GetValues(typeof(Country)).Cast<int>().Contains(countryId))
                    return "Invalid value";
            }
            else
            {
                if (!Enum.IsDefined(typeof(Country), Value))
                    return "Invalid value";
            }

            return null;
        }
    }
}
