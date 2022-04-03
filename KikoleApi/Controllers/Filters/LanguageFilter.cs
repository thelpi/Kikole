using System;
using System.Linq;
using KikoleApi.Models.Enums;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KikoleApi.Controllers.Filters
{
    public class LanguageFilter : IResourceFilter
    {
        const string LanguageHeader = "Language";

        private readonly TextResources _resources;

        public LanguageFilter(TextResources resources)
        {
            _resources = resources;
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // does nothing
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            _resources.Language = Languages.en;
            if (context.HttpContext.Request.Headers.ContainsKey(LanguageHeader))
            {
                var values = context.HttpContext.Request.Headers[LanguageHeader];
                if (values.Count == 1)
                {
                    var value = values[0];
                    if (int.TryParse(value, out var languageId)
                        && Enum.GetValues(typeof(Languages)).Cast<int>().Contains(languageId))
                        _resources.Language = (Languages)languageId;
                    else if (Enum.IsDefined(typeof(Languages), value))
                        _resources.Language = Enum.Parse<Languages>(value);
                }
            }
        }
    }
}
