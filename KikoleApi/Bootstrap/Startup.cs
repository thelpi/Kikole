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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KikoleApi.Bootstrap
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc(options =>
                {
                    options.Filters.Add<AuthorizationFilter>();
                    options.Filters.Add<ControllerErrorFilter>();
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

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
                .AddLocalization(options => options.ResourcesPath = "Resources");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
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
            
            app.UseMvc();

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
