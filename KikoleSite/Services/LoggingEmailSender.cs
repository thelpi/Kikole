using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KikoleSite.Services;

/// <summary>
/// Remplace l'envoi reel en local (cf. "Email:SendingEnabled" a <c>false</c>) : ecrit le
/// contenu dans les logs plutot que d'exiger une vraie boite mail pour tester un lien de
/// confirmation, de changement d'adresse ou de reinitialisation.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toAddress, string subject, string htmlBody)
    {
        _logger.LogInformation(
            "[Email non envoye - developpement] A: {ToAddress} / Sujet: {Subject}\n{Body}",
            toAddress, subject, htmlBody);

        return Task.CompletedTask;
    }
}
