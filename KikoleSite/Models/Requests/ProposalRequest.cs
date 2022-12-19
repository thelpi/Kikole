using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests
{
    public class ProposalRequest
    {
        public uint DaysBeforeNow { get; set; }
        public string Value { get; set; }
        internal string Ip { get; set; }
        internal DateTime ProposalDateTime { get; set; }
        internal ProposalTypes ProposalType { get; set; }

        internal bool IsTodayPlayer => DaysBeforeNow == 0;

        internal DateTime PlayerSubmissionDate => ProposalDateTime.AddDays(-DaysBeforeNow).Date;

        internal string GetTip(PlayerDto player, IStringLocalizer resources)
        {
            return ProposalType switch
            {
                ProposalTypes.Year => ushort.Parse(Value) > player.YearOfBirth
                    ? resources["TipOlderPlayer"]
                    : resources["TipYoungerPlayer"],
                ProposalTypes.Leaderboard => resources["LeaderboardAvailable"],
                ProposalTypes.Clue => resources["ClueAvailable"],
                _ => null,
            };
        }

        internal ProposalDto ToDto(ulong userId, bool successful)
        {
            return new ProposalDto
            {
                ProposalDate = PlayerSubmissionDate,
                Successful = (byte)(successful ? 1 : 0),
                UserId = userId,
                Value = Value?.ToString(),
                ProposalTypeId = (ulong)ProposalType,
                Ip = Ip
            };
        }

        internal bool MatchAny(IEnumerable<ProposalDto> proposals)
        {
            // Assume date and user OK
            return proposals.Any(p =>
                p.ProposalTypeId == (ulong)ProposalType
                && p.Value == Value);
        }
    }
}
