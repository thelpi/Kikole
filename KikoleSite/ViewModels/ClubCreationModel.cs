using System.Collections.Generic;

namespace KikoleSite.ViewModels;

public class ClubCreationModel
{
    public string? ErrorMessage { get; set; }

    public string? InfoMessage { get; set; }

    public string? MainNameEn { get; set; }

    public string? MainNameFr { get; set; }

    /// <summary>Un alias EN par ligne.</summary>
    public string? AlternativeNamesEn { get; set; }

    /// <summary>Un alias FR par ligne.</summary>
    public string? AlternativeNamesFr { get; set; }

    public string? Country { get; set; }

    public ulong Id { get; set; }

    /// <summary>Administrateur : peut rechercher un club existant pour le modifier ou le supprimer.</summary>
    public bool CanEditExisting { get; set; }

    /// <summary>Libelle du pays, affiche dans le champ de saisie (l'identifiant seul est dans <see cref="Country"/>).</summary>
    public string? CountryName { get; set; }

    /// <summary>Kikolés dont la carrière contient le club (20 au plus) : tant qu'il y en a, pas de suppression.</summary>
    public IReadOnlyCollection<string> PlayerNames { get; set; } = [];

    /// <summary>Propositions incorrectes qui garderont le nom du club si on le supprime.</summary>
    public int IncorrectProposals { get; set; }

    /// <summary>Propositions réussies visant le club : elles interdisent aussi la suppression.</summary>
    public int CorrectProposals { get; set; }

    public bool CanDelete => Id > 0 && PlayerNames.Count == 0 && CorrectProposals == 0;
}
