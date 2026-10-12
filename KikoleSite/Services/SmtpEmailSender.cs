using System;
using System.Diagnostics;
using System.Threading.Tasks;
using KikoleSite.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KikoleSite.Services;

public class SmtpEmailSender : IEmailSender
{
    // au-dela, l'envoi (attendu pendant la requete) est journalise en avertissement
    internal const int SlowSendThresholdMs = 3000;

    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toAddress, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();

        var stopwatch = Stopwatch.StartNew();
        var stage = "connect";
        long connectMs, authMs, sendMs;

        try
        {
            await client.ConnectAsync(
                _options.SmtpHost,
                _options.SmtpPort,
                _options.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            connectMs = stopwatch.ElapsedMilliseconds;

            stage = "authenticate";
            if (!string.IsNullOrEmpty(_options.SmtpUser))
                await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword ?? string.Empty);
            authMs = stopwatch.ElapsedMilliseconds - connectMs;

            stage = "send";
            await client.SendAsync(message);
            sendMs = stopwatch.ElapsedMilliseconds - connectMs - authMs;

            stage = "disconnect";
            await client.DisconnectAsync(true);
        }
        catch
        {
            // l'adresse du destinataire n'est volontairement pas journalisee
            _logger.LogWarning("Échec de l'envoi SMTP à l'étape {Stage} après {ElapsedMs} ms.", stage, stopwatch.ElapsedMilliseconds);
            throw;
        }

        var totalMs = stopwatch.ElapsedMilliseconds;
        _logger.Log(
            totalMs > SlowSendThresholdMs ? LogLevel.Warning : LogLevel.Information,
            "Email envoyé en {TotalMs} ms (connexion {ConnectMs} ms, authentification {AuthMs} ms, envoi {SendMs} ms).",
            totalMs, connectMs, authMs, sendMs);
    }
}