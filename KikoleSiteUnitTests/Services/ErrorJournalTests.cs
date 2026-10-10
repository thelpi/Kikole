using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services;

public class ErrorJournalTests
{
    private readonly Mock<IClock> _clock = new();
    private readonly ErrorJournal _journal;

    public ErrorJournalTests()
    {
        _clock.Setup(_ => _.Now).Returns(new DateTime(2026, 10, 11, 9, 30, 0));
        _journal = new ErrorJournal(_clock.Object);
    }

    [Fact]
    public void Add_KeepsTheRequestAndTheException_NewestFirst()
    {
        _journal.Add(new InvalidOperationException("premiere"), "GET", "/a");
        _journal.Add(new ArgumentException("seconde"), "POST", "/b");

        var entries = _journal.GetLatest();

        entries.Select(e => e.Message).Should().Equal("seconde", "premiere");
        entries[0].Should().Match<ErrorJournalEntry>(e =>
            e.Method == "POST" && e.Path == "/b" && e.ExceptionType == "System.ArgumentException"
            && e.Date == new DateTime(2026, 10, 11, 9, 30, 0) && e.Details.Contains("seconde"));
    }

    [Fact]
    public void Add_KeepsOnlyTheMostRecentEntries()
    {
        for (var i = 0; i < ErrorJournal.Capacity + 5; i++)
            _journal.Add(new Exception($"erreur {i}"), "GET", "/");

        var entries = _journal.GetLatest();

        entries.Should().HaveCount(ErrorJournal.Capacity);
        entries[0].Message.Should().Be($"erreur {ErrorJournal.Capacity + 4}");
        entries[^1].Message.Should().Be("erreur 5");
    }

    [Fact]
    public async Task TheExceptionHandler_RecordsTheExceptionAndLeavesTheDefaultHandlingGoOn()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/Quelque/Chose";
        var handler = new ErrorJournalExceptionHandler(_journal);

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("vue"), CancellationToken.None);

        handled.Should().BeFalse();
        _journal.GetLatest().Should().ContainSingle().Which.Path.Should().Be("/Quelque/Chose");
    }
}