using System;
using System.Linq;
using KikoleApi.Models.Enums;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KikoleApi.Controllers.Filters
{
    public class LanguageFilter : IResourceFilter
    {
        const string LanguageHeader = "Language";

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // does nothing
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var resources = SPA.TextResources;
            if (resources == null)
                return;

            SPA.TextResources.Language = Languages.en;
            if (context.HttpContext.Request.Headers.ContainsKey(LanguageHeader))
            {
                var values = context.HttpContext.Request.Headers[LanguageHeader];
                if (values.Count == 1)
                {
                    var value = values[0];
                    if (int.TryParse(value, out var languageId))
                    {
                        if (Enum.GetValues(typeof(Languages)).Cast<int>().Contains(languageId))
                            SPA.TextResources.Language = (Languages)languageId;
                    }
                    else if (Enum.IsDefined(typeof(Languages), value))
                        SPA.TextResources.Language = Enum.Parse<Languages>(value);
                }
            }
        }
    }
}
