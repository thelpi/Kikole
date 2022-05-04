using System;
using System.Collections.Generic;
using System.Globalization;
using KikoleSite.Api.Handlers;
using KikoleSite.Api.Helpers;
using KikoleSite.Api.Interfaces;
using KikoleSite.Api.Interfaces.Handlers;
using KikoleSite.Api.Interfaces.Repositories;
using KikoleSite.Api.Interfaces.Services;
using KikoleSite.Api.Repositories;
using KikoleSite.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KikoleSite
{
    public class Startup
    {
        const ulong DefaultLanguageId = 1;

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
            services.AddMvc(options => options.Filters.Add<ControllerErrorFilter>())
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            services.AddSingleton<IApiProvider, ApiProvider>();

            services
                // repositories
                .AddSingleton<IPlayerRepository, PlayerRepository>()
                .AddSingleton<IClubRepository, ClubRepository>()
                .AddSingleton<IProposalRepository, ProposalRepository>()
                .AddSingleton<IUserRepository, UserRepository>()
                .AddSingleton<IInternationalRepository, InternationalRepository>()
                .AddSingleton<ILeaderRepository, LeaderRepository>()
                .AddSingleton<IBadgeRepository, BadgeRepository>()
                .AddSingleton<IMessageRepository, MessageRepository>()
                .AddSingleton<IChallengeRepository, ChallengeRepository>()
                // handlers
                .AddSingleton<IPlayerHandler, PlayerHandler>()
                // services
                .AddSingleton<IBadgeService, BadgeService>()
                .AddSingleton<IPlayerService, PlayerService>()
                .AddSingleton<ILeaderService, LeaderService>()
                .AddSingleton<IProposalService, ProposalService>()
                .AddSingleton<IChallengeService, ChallengeService>()
                // helpers
                .AddSingleton<ICrypter, Crypter>()
                .AddSingleton<IClock, Clock>()
                .AddSingleton(new Random());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
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

            var cultures = new List<CultureInfo> {
                new CultureInfo("en"),
                new CultureInfo("fr")
            };

            app.UseRequestLocalization(options => {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en");
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;
            });
        }
    }
}
