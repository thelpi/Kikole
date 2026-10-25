using System;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models;

public class Badge
{
    public ulong Id { get; }

    public string Name { get; }

    public string Description { get; }

    public int Users { get; }

    public bool Hidden { get; }

    public BadgeTiers Tier { get; }

    /// <summary>Texte affiche a la place de la description d'un badge a description masquee.</summary>
    public const string MaskedDescription = "?????";

    /// <param name="dto">Badge en base.</param>
    /// <param name="usersCount">Nombre de joueurs détenant le badge.</param>
    /// <param name="translation">Nom et description dans la langue demandée ; à défaut (ou champ vide), ceux du badge (anglais).</param>
    /// <param name="revealDescription">Faux : la description d'un badge à description masquée est remplacée par <see cref="MaskedDescription"/>.</param>
    internal Badge(BadgeDto dto, int usersCount, BadgeTranslationDto? translation, bool revealDescription = true)
    {
        Id = dto.Id;
        Name = string.IsNullOrWhiteSpace(translation?.Name) ? dto.Name : translation.Name;
        Description = dto.DescriptionHidden && !revealDescription
            ? MaskedDescription
            : string.IsNullOrWhiteSpace(translation?.Description) ? dto.Description : translation.Description;
        Users = usersCount;
        Hidden = dto.Hidden > 0;
        Tier = Enum.IsDefined((BadgeTiers)dto.Tier) ? (BadgeTiers)dto.Tier : BadgeTiers.Common;
    }
}
