using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Configuration;
using KikoleSite.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace KikoleSiteUnitTests.Services;

public class SmtpEmailSenderTests
{
    private const string Recipient = "destinataire@kikole.test";

    private sealed class CapturingLogger : ILogger<SmtpEmailSender>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    // serveur SMTP minimal : accepte une connexion, accepte tout, puis repond a QUIT
    private static (int Port, Task Served) StartFakeSmtpServer()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        async Task ServeAsync()
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true };

                await writer.WriteLineAsync("220 localhost ESMTP");
                var inData = false;
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (inData)
                    {
                        if (line == ".")
                        {
                            inData = false;
                            await writer.WriteLineAsync("250 OK");
                        }
                        continue;
                    }

                    var command = line.ToUpperInvariant();
                    if (command.StartsWith("DATA"))
                    {
                        await writer.WriteLineAsync("354 go");
                        inData = true;
                    }
                    else if (command.StartsWith("QUIT"))
                    {
                        await writer.WriteLineAsync("221 bye");
                        break;
                    }
                    else
                    {
                        await writer.WriteLineAsync("250 localhost");
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        return (port, ServeAsync());
    }

    private static SmtpEmailSender Sender(int port, CapturingLogger logger) => new(
        Options.Create(new EmailOptions
        {
            FromAddress = "no-reply@kikole.test",
            SmtpHost = "127.0.0.1",
            SmtpPort = port,
            SmtpUseSsl = false
        }),
        logger);

    [Fact]
    public async Task SendAsync_LogsTheDurationOfAFastSendAsInformation()
    {
        var (port, served) = StartFakeSmtpServer();
        var logger = new CapturingLogger();

        await Sender(port, logger).SendAsync(Recipient, "Sujet", "<p>Bonjour</p>");
        await served;

        var entry = logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().StartWith("Email envoyé en ").And.Contain("connexion").And.Contain("authentification").And.Contain("envoi");
        entry.Message.Should().NotContain(Recipient);
    }

    [Fact]
    public async Task SendAsync_WhenTheServerIsUnreachable_LogsTheStageAndRethrows()
    {
        var closed = new TcpListener(IPAddress.Loopback, 0);
        closed.Start();
        var port = ((IPEndPoint)closed.LocalEndpoint).Port;
        closed.Stop();
        var logger = new CapturingLogger();

        var send = () => Sender(port, logger).SendAsync(Recipient, "Sujet", "<p>Bonjour</p>");

        await send.Should().ThrowAsync<Exception>();
        var entry = logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("à l'étape connect").And.Contain(" ms");
        entry.Message.Should().NotContain(Recipient);
    }
}