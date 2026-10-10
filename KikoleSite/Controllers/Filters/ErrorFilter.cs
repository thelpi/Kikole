using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KikoleSite.Controllers.Filters;

public class ErrorFilter : IExceptionFilter
{
    private readonly IClock _clock;
    private readonly ILogger<ErrorFilter> _logger;
    private readonly string? _logsFilePathFormat;

    public ErrorFilter(IConfiguration configuration,
        IClock clock,
        ILogger<ErrorFilter> logger)
    {
        _logsFilePathFormat = configuration.GetValue<string>("LogsFilePathFormat");
        _clock = clock;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        // marquer l'exception comme traitee la rend invisible pour le reste du pipeline : sans cette
        // ligne, un conteneur (sans chemin de fichier configure) ne laisserait aucune trace
        _logger.LogError(context.Exception, "Exception non gérée sur {Path}", context.HttpContext.Request.Path);

        // le chemin des logs est optionnel : absent, on se contente d'afficher l'erreur
        if (_logsFilePathFormat != null)
        {
            try
            {
                var now = _clock.Now;
                var logFileName = string.Format(_logsFilePathFormat, now.ToString("yyyyMMdd"));
                using var sw = new StreamWriter(logFileName, true);
                sw.WriteLine($"Exception timestamp: {now:HH:mm:ss}");
                sw.WriteLine(context.Exception.Message);
                sw.WriteLine(context.Exception.StackTrace);
                sw.WriteLine(sw.NewLine);
            }
            catch { }
        }
        context.ExceptionHandled = true;
        context.Result = new ViewResult { ViewName = "~/Views/Shared/Error.cshtml" };
    }
}
