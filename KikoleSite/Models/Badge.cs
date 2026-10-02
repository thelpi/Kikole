using KikoleSite.Models.Dtos;

namespace KikoleSite.Models;

public class Badge
{
    public ulong Id { get; }

    public string Name { get; }

    public string Description { get; }

    public int Users { get; }

    public bool Hidden { get; }

    /// <param name="dto">Badge en base.</param>
    /// <param name="usersCount">Nombre de joueurs détenant le badge.</param>
    /// <param name="translation">Nom et description dans la langue demandée ; à défaut (ou champ vide), ceux du badge (anglais).</param>
    internal Badge(BadgeDto dto, int usersCount, BadgeTranslationDto? translation)
    {
        Id = dto.Id;
        Name = string.IsNullOrWhiteSpace(translation?.Name) ? dto.Name : translation.Name;
        Description = string.IsNullOrWhiteSpace(translation?.Description) ? dto.Description : translation.Description;
        Users = usersCount;
        Hidden = dto.Hidden > 0;
    }
}
