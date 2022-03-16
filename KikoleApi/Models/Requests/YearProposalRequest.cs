﻿using KikoleApi.Models.Dtos;

namespace KikoleApi.Models.Requests
{
    public class YearProposalRequest : BaseProposalRequest
    {
        internal override ProposalType ProposalType => ProposalType.Year;

        internal override string GetTip(PlayerDto player)
        {
            return $"The player is {(ushort.Parse(Value) > player.YearOfBirth ? "older" : "younger")}";
        }

        internal override string IsValid()
        {
            if (!ushort.TryParse(Value, out _))
                return "Invalid value";

            return null;
        }
    }
}