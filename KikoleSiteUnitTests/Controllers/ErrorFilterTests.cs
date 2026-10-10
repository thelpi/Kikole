using System;
using System.Collections.Generic;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Controllers.Filters;
using KikoleSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Controllers;

public class ErrorFilterTests
{
    private sealed class CapturingLogger : ILogger<ErrorFilter>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, exception));
    }

    [Fact]
    public void OnException_WithoutLogFilePath_StillLogsTheExceptionAndRendersTheErrorView()
    {
        var logger = new CapturingLogger();
        var clock = new Mock<IClock>();
        var journal = new Mock<IErrorJournal>();
        var filter = new ErrorFilter(new ConfigurationBuilder().Build(), clock.Object, logger, journal.Object);
        var boom = new InvalidOperationException("boom");
        var context = new ExceptionContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>())
        {
            Exception = boom
        };

        filter.OnException(context);

        logger.Entries.Should().ContainSingle().Which.Should().Be((LogLevel.Error, (Exception?)boom));
        journal.Verify(_ => _.Add(boom, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        context.ExceptionHandled.Should().BeTrue();
        context.Result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("~/Views/Shared/Error.cshtml");
    }
}
