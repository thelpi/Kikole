using System;
using System.Collections.Generic;
using System.Globalization;
using KikoleSite;
using KikoleSite.Controllers.Filters;
using KikoleSite.Handlers;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services
    .AddMvc(options =>
    {
        options.Filters.Add<ErrorFilter>();
        options.Filters.Add<AuthorizationFilter>();
    })
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.AddHttpContextAccessor();

builder.Services
    // repositories
    .AddSingleton<IPlayerRepository, PlayerRepository>()
    .AddSingleton<IClubRepository, ClubRepository>()
    .AddSingleton<IProposalRepository, ProposalRepository>()
    .AddSingleton<IUserRepository, UserRepository>()
    .AddSingleton<IInternationalRepository, InternationalRepository>()
    .AddSingleton<ILeaderRepository, LeaderRepository>()
    .AddSingleton<IBadgeRepository, BadgeRepository>()
    .AddSingleton<IMessageRepository, MessageRepository>()
    .AddSingleton<IDiscussionRepository, DiscussionRepository>()
    .AddSingleton<IStatisticRepository, StatisticRepository>()
    // handlers
    .AddSingleton<IPlayerHandler, PlayerHandler>()
    // services
    .AddSingleton<IBadgeService, BadgeService>()
    .AddSingleton<IPlayerService, PlayerService>()
    .AddSingleton<ILeaderService, LeaderService>()
    .AddSingleton<IProposalService, ProposalService>()
    .AddSingleton<IStatisticService, StatisticService>()
    .AddSingleton<IInternationalService, InternationalService>()
    // helpers
    .AddSingleton<ICrypter, Crypter>()
    .AddSingleton<IClock, Clock>()
    .AddSingleton(new Random());

var app = builder.Build();

var cultures = new List<CultureInfo>
{
    new("en"),
    new("fr")
};

app.UseRequestLocalization(options =>
{
    options.DefaultRequestCulture = new RequestCulture("fr");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
});

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
