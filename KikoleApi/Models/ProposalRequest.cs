using System;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ProposalRequest
    {
        public ProposalType ProposalType { get; set; }

        public string Value { get; set; }

        public DateTime ProposalDate { get; set; }

        internal string IsValid()
        {
            if (!Enum.IsDefined(typeof(ProposalType), ProposalType))
                return "Invalid proposal type";

            switch (ProposalType)
            {
                case ProposalType.Club:
                case ProposalType.Name:
                    if (string.IsNullOrWhiteSpace(Value))
                        return "Invalid value";
                    break;
                case ProposalType.Country:
                    if (!Enum.TryParse<Country>(Value, out _))
                        return "Invalid country value";
                    break;
                case ProposalType.Year:
                    if (!int.TryParse(Value, out _))
                        return "Invalid year value";
                    break;
            }

            return null;
        }

        internal ProposalDto ToDto(ulong userId, bool successful)
        {
            return new ProposalDto
            {
                ProposalDate = ProposalDate,
                ProposalTypeId = (ulong)ProposalType,
                Successful = (byte)(successful ? 1 : 0),
                UserId = userId,
                Value = Value
            };
        }
    }
}
