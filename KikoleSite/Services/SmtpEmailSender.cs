using System.Threading.Tasks;
using KikoleSite.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KikoleSite.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string toAddress, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _options.SmtpHost,
            _options.SmtpPort,
            _options.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        if (!string.IsNullOrEmpty(_options.SmtpUser))
            await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword ?? string.Empty);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
