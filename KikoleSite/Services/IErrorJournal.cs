using System;
using System.Collections.Generic;

namespace KikoleSite.Services;

/// <summary>Une erreur non gérée, telle qu'affichée dans la page d'administration.</summary>
public sealed record ErrorJournalEntry(
    DateTime Date,
    string Method,
    string Path,
    string ExceptionType,
    string Message,
    string Details);

/// <summary>
/// Les dernières erreurs non gérées, gardées en mémoire (perdues au redémarrage) pour que
/// l'administrateur les lise sans accès au serveur. Les erreurs vont aussi dans le journal
/// du conteneur.
/// </summary>
public interface IErrorJournal
{
    void Add(Exception exception, string? requestMethod, string? requestPath);

    /// <summary>Les erreurs gardées, la plus récente d'abord.</summary>
    IReadOnlyList<ErrorJournalEntry> GetLatest();
}