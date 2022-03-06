﻿using System.IO;
using System.Net;
using KikoleApi.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Filters
{
    public class ControllerErrorFilter : IExceptionFilter
    {
        private readonly IClock _clock;
        private readonly string _logsFilePathFormat;

        public ControllerErrorFilter(IConfiguration configuration,
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
                    using (var sw = new StreamWriter(logFileName, true))
                    {
                        sw.WriteLine($"Exception timestamp: {now.ToString("hh:mm:ss")}");
                        sw.WriteLine(context.Exception.Message);
                        sw.WriteLine(context.Exception.StackTrace);
                        sw.WriteLine(sw.NewLine);
                    }
                    context.Exception = null;
                    context.ExceptionHandled = true;
                    context.Result = new StatusCodeResult((int)HttpStatusCode.InternalServerError);
                }
                catch { }
            }
        }
    }
}
