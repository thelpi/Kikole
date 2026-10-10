using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Services;

public class ErrorJournal : IErrorJournal
{
    internal const int Capacity = 100;
    private const int MaxDetailsLength = 8000;

    private readonly IClock _clock;
    private readonly Queue<ErrorJournalEntry> _entries = new();
    private readonly object _lock = new();

    public ErrorJournal(IClock clock)
    {
        _clock = clock;
    }

    public void Add(Exception exception, string? requestMethod, string? requestPath)
    {
        var details = exception.ToString();
        if (details.Length > MaxDetailsLength)
            details = details[..MaxDetailsLength];

        var entry = new ErrorJournalEntry(
            _clock.Now,
            requestMethod ?? string.Empty,
            requestPath ?? string.Empty,
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message,
            details);

        lock (_lock)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > Capacity)
                _entries.Dequeue();
        }
    }

    public IReadOnlyList<ErrorJournalEntry> GetLatest()
    {
        lock (_lock)
        {
            return [.. _entries.Reverse()];
        }
    }
}