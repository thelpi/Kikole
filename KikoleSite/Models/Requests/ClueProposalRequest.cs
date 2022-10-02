﻿using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests
{
    public class ClueProposalRequest : BaseProposalRequest
    {
        internal override ProposalTypes ProposalType => ProposalTypes.Clue;

        internal override string IsValid(IStringLocalizer resources)
        {
            return null;
        }

        internal override string GetTip(PlayerDto player, IStringLocalizer resources)
        {
            return resources["ClueAvailable"];
        }
    }
}