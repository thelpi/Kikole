namespace KikoleSite.Models.Dtos;

public record BadgeDto : BaseDto
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public byte Hidden { get; init; }

    /// <summary>Rarete (1 commun, 2 rare, 3 epique, 4 legendaire), cf. <see cref="Enums.BadgeTiers"/>.</summary>
    public byte Tier { get; init; } = (byte)Enums.BadgeTiers.Common;

    /// <summary>Suppression virtuelle : un badge desactive n'apparait plus nulle part
    /// (catalogue, badges d'un utilisateur, attribution), mais ses lignes <c>user_badges</c>
    /// existantes ne sont jamais effacees. Piloté uniquement en base, pas de backoffice.</summary>
    public bool IsDisabled { get; init; }
}
