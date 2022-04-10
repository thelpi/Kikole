﻿using System;
using KikoleApi.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace KikoleApi.Helpers
{
    internal static class LocalizationHelper
    {
        internal static Languages ExtractLanguage(this IHttpContextAccessor hca)
        {
            var acceptLanguage = hca.HttpContext.Request.GetTypedHeaders().AcceptLanguage;
            if (acceptLanguage?.Count > 0)
            {
                if (Enum.TryParse<Languages>(acceptLanguage[0].Value.Value, out var language))
                {
                    return language;
                }
            }
            return Languages.en;
        }
    }
}