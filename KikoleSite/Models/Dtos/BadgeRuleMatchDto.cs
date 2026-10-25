using System;

namespace KikoleSite.Models.Dtos;

/// <summary>Une ligne renvoyee par la requete d'un badge (<see cref="BadgeDto.ConditionSql"/>) :
/// l'utilisateur qui remplit la condition et la date d'obtention.</summary>
public record BadgeRuleMatchDto
{
    public ulong UserId { get; init; }

    public DateOnly BadgeDate { get; init; }
}