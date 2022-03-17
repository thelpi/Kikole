using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models.Requests
{
    public abstract class BaseProposalRequest
    {
        public DateTime ProposalDate { get; set; }

        public uint DaysBefore { get; set; }

        public string Value { get; set; }

        public int SourcePoints { get; set; }

        internal abstract ProposalTypes ProposalType { get; }

        internal virtual string GetTip(PlayerDto player)
        {
            return null;
        }

        internal virtual string IsValid()
        {
            if (string.IsNullOrWhiteSpace(Value))
                return "Invalid value";

            return null;
        }

        internal ProposalDto ToDto(ulong userId, bool successful)
        {
            return new ProposalDto
            {
                ProposalDate = ProposalDate,
                Successful = (byte)(successful ? 1 : 0),
                UserId = userId,
                Value = Value?.ToString(),
                ProposalTypeId = (ulong)ProposalType,
                DaysBefore = DaysBefore
            };
        }

        internal DateTime PlayerSubmissionDate => ProposalDate.AddDays(-DaysBefore);
    }
}
