namespace KikoleSite.Models.Dtos;

/// <summary>Nom et description d'un badge dans une langue (table <c>badge_translations</c>).</summary>
public record BadgeTranslationDto
{
    public required string Name { get; init; }

    public required string Description { get; init; }
}