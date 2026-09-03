using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Helpers;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests;

public record ProposalRequest
{
    public uint DaysBeforeNow { get; init; }
    public string? Value { get; init; }
    internal string? Ip { get; init; }
    internal DateTime ProposalDateTime { get; init; }
    internal ProposalTypes ProposalType { get; init; }

    internal bool IsTodayPlayer => DaysBeforeNow == 0;

    internal DateTime PlayerSubmissionDate => ProposalDateTime.AddDays(-DaysBeforeNow).Date;

    internal string? GetTip(PlayerDto player, IStringLocalizer resources)
    {
        return ProposalType switch
        {
            ProposalTypes.Year => ushort.Parse(Value!) > player.YearOfBirth
                ? resources["TipOlderPlayer"].Value
                : resources["TipYoungerPlayer"].Value,
            ProposalTypes.Leaderboard => resources["LeaderboardAvailable"].Value,
            ProposalTypes.Clue => resources["ClueAvailable"].Value,
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
        // la comparaison est sanitisee comme celle qui decide de la reussite :
        // sans ca "Zidane", "ZIDANE" et "Zidàne" seraient factures separement
        var sanitizedValue = Value?.Sanitize();
        return proposals.Any(p =>
            p.ProposalTypeId == (ulong)ProposalType
            && p.Value?.Sanitize() == sanitizedValue);
    }
}
