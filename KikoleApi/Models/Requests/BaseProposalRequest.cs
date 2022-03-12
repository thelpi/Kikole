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

        public string UserIp { get; set; }

        internal abstract ProposalType ProposalType { get; }

        internal virtual string GetTip(PlayerDto player)
        {
            return null;
        }

        internal virtual string IsValid()
        {
            if (string.IsNullOrWhiteSpace(Value))
                return "Invalid value";

            if (string.IsNullOrWhiteSpace(UserIp))
                return "IP is empty";

            return null;
        }

        internal ProposalDto ToDto(ulong userId, bool successful, DateTime timestamp)
        {
            return new ProposalDto
            {
                ProposalDate = ProposalDate,
                Successful = (byte)(successful ? 1 : 0),
                UserId = userId,
                Value = Value?.ToString(),
                ProposalTypeId = (ulong)ProposalType,
                DaysBefore = DaysBefore,
                Ip = UserIp,
                CreationDate = timestamp
            };
        }

        internal DateTime PlayerSubmissionDate => ProposalDate.AddDays(-DaysBefore);
    }
}
