using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Helpers;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests;

public record PlayerRequest
{
    public required string Name { get; init; }

    public ushort YearOfBirth { get; init; }

    public Countries Country { get; init; }

    public Continents Continent { get; init; }

    public DateTime? PublicationDate { get; init; }

    public required IReadOnlyList<string> AllowedNames { get; init; }

    public required IReadOnlyList<PlayerClubRequest> Clubs { get; init; }

    public required string ClueEn { get; init; }

    public required string EasyClueEn { get; init; }

    public required IReadOnlyDictionary<Languages, string?> ClueLanguages { get; init; }

    public required IReadOnlyDictionary<Languages, string?> EasyClueLanguages { get; init; }

    internal Positions Position { get; init; }

    public bool SetLatestPublicationDate { get; init; }

    public bool HideCreator { get; init; }

    internal string? IsValid(DateTime today, IStringLocalizer resources)
    {
        if (string.IsNullOrWhiteSpace(Name))
            return resources["InvalidName"];

        if (YearOfBirth < 1850 || YearOfBirth > 2100)
            return resources["InvalidBirthYear"];

        if (!AllowedNames.IsValid())
            return resources["InvalidAllowedNames"];

        if (Clubs.Count == 0)
            return resources["EmptyClubsList"];

        if (Clubs.Any(c => c.ClubId == 0))
            return resources["InvalidClubs"];

        var historyCheck = Clubs.Select(c => c.HistoryPosition);
        if (historyCheck.Distinct().Count() != Clubs.Count
            || historyCheck.Min() != 1
            || historyCheck.Max() - Clubs.Count != 0)
            return resources["InvalidClubs"];

        if (string.IsNullOrWhiteSpace(ClueEn) || string.IsNullOrWhiteSpace(EasyClueEn))
            return resources["InvalidClue"];

        if (PublicationDate.HasValue && PublicationDate.Value.Date < today)
            return resources["InvalidPublicationDate"];

        return null;
    }

    /// <summary>
    /// La date de parution est passee en argument plutot que lue sur la requete :
    /// elle peut etre calculee par le service quand la requete n'en porte pas.
    /// </summary>
    internal PlayerDto ToDto(ulong userId, DateTime? publicationDate)
    {
        return new PlayerDto
        {
            ContinentId = (ulong)Continent,
            CountryId = (ulong)Country,
            Name = Name,
            PublicationDate = publicationDate,
            YearOfBirth = YearOfBirth,
            AllowedNames = AllowedNames.SanitizeJoin(Name),
            Clue = ClueEn,
            EasyClue = EasyClueEn,
            PositionId = (ulong)Position,
            CreationUserId = userId,
            HideCreator = (byte)(HideCreator ? 1 : 0)
        };
    }

    internal IReadOnlyList<PlayerClubDto> ToPlayerClubDtos(ulong playerId)
    {
        return Clubs
            .Select(c => c.ToPlayerClubDto(playerId))
            .ToList();
    }
}
