using System;
using System.Collections.Generic;
using System.Globalization;
using KikoleApi.Controllers.Filters;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Interfaces.Repositories;
using KikoleApi.Interfaces.Services;
using KikoleApi.Repositories;
using KikoleApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KikoleApi.Bootstrap
{
    /// <summary>
    /// Startup.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="configuration"><see cref="Configuration"/>.</param>
        /// <param name="environment"><see cref="Environment"/>.</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        /// <summary>
        /// Configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Environment.
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Services collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc(options =>
                {
                    options.Filters.Add<AuthorizationFilter>();
                    options.Filters.Add<ControllerErrorFilter>();
                });

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen();

            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

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
                // services
                .AddSingleton<IBadgeService, BadgeService>()
                .AddSingleton<IPlayerService, PlayerService>()
                .AddSingleton<ILeaderService, LeaderService>()
                .AddSingleton<IProposalService, ProposalService>()
                .AddSingleton<IChallengeService, ChallengeService>()
                // helpers
                .AddSingleton<ICrypter, Crypter>()
                .AddSingleton<IClock, Clock>()
                .AddHttpContextAccessor()
                .AddLocalization(options => options.ResourcesPath = "Resources")
                .AddSingleton(new Random());
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">App builder.</param>
        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
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
