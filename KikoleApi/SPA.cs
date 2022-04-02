using System;
using Microsoft.Extensions.DependencyInjection;

namespace KikoleApi
{
    public class SPA
    {
        private static IServiceProvider _provider;

        public static void SetProvider(IServiceProvider provider)
        {
            _provider = provider;
        }

        public static TextResources TextResources => (TextResources)_provider.CreateScope().ServiceProvider.GetService(typeof(TextResources));
    }
}
