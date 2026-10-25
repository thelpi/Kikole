using System.Collections.Generic;

namespace KikoleSite.Models;

public enum ClubDeletionOutcomes
{
    Deleted,
    NotFound,
    UsedByPlayers,
    UsedByCorrectProposals
}

/// <summary>Issue d'une suppression de club ; <paramref name="ConvertedProposals"/> compte les propositions incorrectes qui ont gardé le nom du club à la place de son identifiant.</summary>
public record ClubDeletionResult(ClubDeletionOutcomes Outcome, int ConvertedProposals = 0);

/// <summary>Ce qui empêche (ou accompagne) la suppression d'un club : kikolés qui l'utilisent et propositions qui le visent.</summary>
public record ClubUsage(IReadOnlyCollection<string> PlayerNames, int IncorrectProposals, int CorrectProposals);