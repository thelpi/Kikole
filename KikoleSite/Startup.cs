using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KikoleSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            StaticConfig = configuration;
            Environment = environment;
        }

        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }
        public static IConfiguration StaticConfig { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
            });

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddMvc()
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            services.AddHttpContextAccessor();

            services
                .Configure<Configurations.TheEliteWebsiteConfiguration>(Configuration.GetSection("TheEliteWebsite"))
                .Configure<Configurations.RankingConfiguration>(Configuration.GetSection("Ranking"))
                .AddSingleton<Repositories.IReadRepository, Repositories.ReadRepository>()
                .AddSingleton<Repositories.IWriteRepository, Repositories.WriteRepository>()
                .AddSingleton<Repositories.ITheEliteWebSiteParser, Repositories.TheEliteWebSiteParser>()
                .AddSingleton<Providers.IStatisticsProvider, Providers.StatisticsProvider>()
                .AddSingleton<Providers.IIntegrationProvider, Providers.IntegrationProvider>()
                .AddSingleton<Loggers.FileLogger>()
                .AddHostedService<Workers.IntegrationWorker>()
                .AddSingleton<Repositories.ICacheManager, Repositories.CacheManager>()
                .AddSingleton<IClock, Clock>()
                .AddSingleton(new Random());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            var cultures = new List<CultureInfo> {
                new CultureInfo("en"),
                new CultureInfo("fr")
            };

            app.UseRequestLocalization(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("fr");
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Ranking}/{action=Index}");
            });
        }
    }
}
