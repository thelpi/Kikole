using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Controllers.Filters
{
    public class ErrorFilter : IExceptionFilter
    {
        private readonly IClock _clock;
        private readonly string _logsFilePathFormat;

        public ErrorFilter(IConfiguration configuration,
            IClock clock)
        {
            _logsFilePathFormat = configuration.GetValue<string>("LogsFilePathFormat");
            _clock = clock;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception != null)
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
            context.Exception = null;
            context.ExceptionHandled = true;
            context.Result = new ViewResult { ViewName = "~/Views/Shared/Error.cshtml" };
        }
    }
}
