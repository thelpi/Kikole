using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests;

public record PlayerSubmissionValidationRequest
{
    public ulong PlayerId { get; init; }

    public bool IsAccepted { get; init; }

    public required IReadOnlyDictionary<Languages, string?> ClueEditLanguages { get; init; }

    public required IReadOnlyDictionary<Languages, string?> EasyClueEditLanguages { get; init; }

    public required string? ClueEditEn { get; init; }

    public required string? EasyClueEditEn { get; init; }

    public required string? RefusalReason { get; init; }

    internal string? IsValid(IStringLocalizer resources)
    {
        if (PlayerId == 0)
            return resources["InvalidPlayerId"];

        if (!IsAccepted && string.IsNullOrWhiteSpace(RefusalReason))
            return resources["RefusalWithoutReason"];

        if (IsAccepted)
        {
            if (ClueEditLanguages?.ContainsKey(Languages.fr) != true
                || ClueEditLanguages.Values.Any(cel => string.IsNullOrWhiteSpace(cel)))
                return resources["InvalidClue"];

            if (EasyClueEditLanguages?.ContainsKey(Languages.fr) != true
                || EasyClueEditLanguages.Values.Any(cel => string.IsNullOrWhiteSpace(cel)))
                return resources["InvalidClue"];
        }

        return null;
    }
}
