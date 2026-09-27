using System.Threading.Tasks;

namespace KikoleSite.Services;

/// <summary>
/// Envoi d'un email HTML. Deux implementations : <see cref="SmtpEmailSender"/> (reelle,
/// MailKit) et <see cref="LoggingEmailSender"/> (developpement local, cf. la cle de
/// configuration "Email:SendingEnabled").
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string htmlBody);
}
