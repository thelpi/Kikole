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
                // helpers
                .AddSingleton<IClock, Clock>()
                .AddSingleton(new Random());

            // elite
            services
                .Configure<Elite.Configurations.TheEliteWebsiteConfiguration>(Configuration.GetSection("TheEliteWebsite"))
                .Configure<Elite.Configurations.RankingConfiguration>(Configuration.GetSection("Ranking"))
                .AddSingleton<Elite.Repositories.IReadRepository, Elite.Repositories.ReadRepository>()
                .AddSingleton<Elite.Repositories.IWriteRepository, Elite.Repositories.WriteRepository>()
                .AddSingleton<Elite.Repositories.ITheEliteWebSiteParser, Elite.Repositories.TheEliteWebSiteParser>()
                .AddSingleton<Elite.Providers.IStatisticsProvider, Elite.Providers.StatisticsProvider>()
                .AddSingleton<Elite.Providers.IIntegrationProvider, Elite.Providers.IntegrationProvider>()
                .AddSingleton<Elite.Loggers.FileLogger>()
                .AddHostedService<Elite.Workers.IntegrationWorker>()
                .AddSingleton<Elite.Repositories.ICacheManager, Elite.Repositories.CacheManager>();
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
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
