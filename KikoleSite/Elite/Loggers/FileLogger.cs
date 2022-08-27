using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Elite.Loggers
{
    public class FileLogger
    {
        private readonly Api.Interfaces.IClock _clock;
        private readonly string _logsFilePathFormat;

        public FileLogger(IConfiguration configuration, Api.Interfaces.IClock clock)
        {
            _logsFilePathFormat = configuration.GetValue<string>("TheEliteLogsFilePathFormat");
            _clock = clock;
        }

        public void Log(Exception ex)
        {
            try
            {
                var now = _clock.Now;
                using var sw = InitiateOrReuseFileStream(now);
                sw.WriteLine($"Exception timestamp: {now:HH:mm:ss}");
                sw.WriteLine(ex.Message);
                sw.WriteLine(ex.StackTrace);
                sw.WriteLine(sw.NewLine);
            }
            catch { }
        }

        public void Log(string message)
        {
            try
            {
                var now = _clock.Now;
                using var sw = InitiateOrReuseFileStream(now);
                sw.WriteLine($"{now:HH:mm:ss} - {message}");
            }
            catch { }
        }

        public void Log(params string[] messages)
        {
            try
            {
                if (messages?.Length > 0)
                {
                    var now = _clock.Now;
                    using var sw = InitiateOrReuseFileStream(now);
                    foreach (var message in messages)
                        sw.WriteLine($"{now:HH:mm:ss} - {message}");
                }
            }
            catch { }
        }

        private StreamWriter InitiateOrReuseFileStream(DateTime now)
        {
            var logFileName = string.Format(_logsFilePathFormat, now.ToString("yyyyMMdd"));
            var sw = new StreamWriter(logFileName, true);
            return sw;
        }
    }
}
