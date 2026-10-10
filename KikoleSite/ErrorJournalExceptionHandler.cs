using System.Threading;
using System.Threading.Tasks;
using KikoleSite.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace KikoleSite;

/// <summary>
/// Note dans le journal les exceptions qui échappent aux filtres MVC (rendu d'une vue,
/// intergiciels...), puis laisse la gestion d'erreur habituelle (<c>/Home/Error</c>) se poursuivre.
/// </summary>
public class ErrorJournalExceptionHandler : IExceptionHandler
{
    private readonly IErrorJournal _journal;

    public ErrorJournalExceptionHandler(IErrorJournal journal)
    {
        _journal = journal;
    }

    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, System.Exception exception, CancellationToken cancellationToken)
    {
        _journal.Add(exception, httpContext.Request.Method, httpContext.Request.Path);

        return ValueTask.FromResult(false);
    }
}