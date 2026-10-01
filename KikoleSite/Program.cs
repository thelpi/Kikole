using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using KikoleSite;
using KikoleSite.Configuration;
using KikoleSite.Controllers.Filters;
using KikoleSite.Handlers;
using KikoleSite.Identity;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
// Dapper n'a pas de support natif de DateOnly (cf. commentaire sur DateOnlyTypeHandler) :
// sans cet enregistrement, toute colonne DATE lue en DateOnly revient silencieusement a
// default/null plutot que de lever une exception.
var dateOnlyTypeHandler = new DateOnlyTypeHandler();
Dapper.SqlMapper.AddTypeHandler(typeof(DateOnly), dateOnlyTypeHandler);
Dapper.SqlMapper.AddTypeHandler(typeof(DateOnly?), dateOnlyTypeHandler);

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
    .AddSingleton<IDiscussionService, DiscussionService>()
    // le chargeur a besoin du type concret, pour appeler un Initialize que l'interface
    // n'expose pas : on enregistre donc GameCalendar, et l'interface renvoie la meme
    // instance. Deux enregistrements independants donneraient deux instances, dont une
    // seule serait amorcee.
    .AddSingleton<GameCalendar>()
    .AddSingleton<IGameCalendar>(sp => sp.GetRequiredService<GameCalendar>())
    .AddHostedService<GameCalendarLoader>()
    // helpers
    .AddSingleton<IClock, Clock>()
    // seedable dans les tests (new Random(seed)) : PlayerService en a besoin pour un
    // melange deterministe, contrairement a Random.Shared qui n'est pas configurable.
    .AddSingleton(new Random())
    // rendu des templates d'email (Views/Emails) en dehors d'une requete MVC — Scoped
    // (pas Singleton) car ITempDataProvider, dont il depend, est lui-meme Scoped.
    .AddScoped<IRazorViewRenderer, RazorViewRenderer>();

// sections de configuration liees via le pattern standard IOptions<T> : les cles attendues
// sont visibles au typage plutot que dispersees en chaines dans chaque classe qui en a
// besoin (a la difference d'IConfiguration injecte brut, comme pour EncryptionKey ou
// HibpApiBaseUrl plus bas — a faire suivre le meme chemin plus tard si l'occasion se
// presente, pas dans ce chantier).
builder.Services
    .Configure<RegistrationOptions>(builder.Configuration.GetSection("Registration"))
    .Configure<ForwardedProxyOptions>(builder.Configuration.GetSection("ForwardedProxy"))
    .Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

// authentification : Identity avec un store Dapper maison (KikoleSite/Identity), pas
// EF Core — le projet n'a jamais eu qu'un seul acces aux donnees. Inscription par email,
// confirmee par lien (RequireConfirmedEmail) ; recuperation de mot de passe par email
// egalement (jeton auto-encode via GeneratePasswordResetTokenAsync), plus de question
// secrete. Ni 2FA, ni telephone.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // la longueur prime sur la composition (NIST 800-63B, OWASP ASVS) : un mot de
        // passe long sans regle de melange resiste mieux qu'un court qui les respecte
        // toutes, parce que les humains satisfont ces regles de facon previsible
        // (majuscule au debut, chiffre a la fin, "!" pour finir — un motif que les
        // dictionnaires de cassage connaissent). Pas de comptes existants a ce jour :
        // aucune raison de ne pas partir sur cette base tout de suite.
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 10;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // tant que l'adresse n'est pas confirmee, la connexion echoue (SignInResult.IsNotAllowed) :
        // c'est ce seul indicateur qui bloque un compte fraichement cree et pas encore confirme.
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddUserStore<DapperUserStore>()
    .AddClaimsPrincipalFactory<UserTypeClaimsPrincipalFactory>()
    .AddDefaultTokenProviders()
    .AddSignInManager();

// duree de vie partagee des liens de confirmation d'inscription, de changement d'adresse
// et de reinitialisation de mot de passe (tous les trois passent par le token provider par
// defaut d'Identity, deja enregistre par AddDefaultTokenProviders ci-dessus).
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    var tokenLifetimeHours = builder.Configuration.GetValue("Email:TokenLifetimeHours", 24);
    options.TokenLifespan = TimeSpan.FromHours(tokenLifetimeHours);
});

builder.Services.AddSingleton<IEmailProtector, EmailProtector>();

// en local, le lien est ecrit dans les logs plutot qu'envoye (cf. EmailOptions.SendingEnabled,
// desactive dans appsettings.Development.json) : pas besoin d'une vraie boite mail pour tester.
if (builder.Configuration.GetValue("Email:SendingEnabled", true))
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();

// verifie les mots de passe contre les fuites connues (Have I Been Pwned, k-anonymity) ;
// s'ajoute au validateur de longueur d'Identity, ne le remplace pas (IPasswordValidator
// supporte plusieurs implementations, executees toutes a chaque changement de mot de
// passe). Timeout court + repli tolerant dans le validateur : l'API tierce ne doit jamais
// bloquer un joueur.
// "HibpCheckEnabled" (defaut vrai) permet de la couper : sur un poste dont l'acces a l'API est
// bloque ou lent, chaque creation de compte / changement de mot de passe attend le timeout
// (3 s) avant de laisser passer - desactive dans appsettings.Development.json.
if (builder.Configuration.GetValue("HibpCheckEnabled", true))
{
    var hibpApiBaseUrl = builder.Configuration.GetValue<string>("HibpApiBaseUrl")
        ?? "https://api.pwnedpasswords.com/";
    builder.Services.AddHttpClient(nameof(HibpPasswordValidator), client =>
    {
        client.BaseAddress = new Uri(hibpApiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(3);
    });
    builder.Services.AddScoped<IPasswordValidator<ApplicationUser>, HibpPasswordValidator>();
}

// remplace le normaliseur par defaut (majuscules seules) par celui qui applique la meme
// regle de deduplication que le reste du projet (StringHelper.Sanitize).
builder.Services.AddSingleton<ILookupNormalizer, SanitizingLookupNormalizer>();

// pas de builder fluent pour le hasher : enregistrement direct, apres AddIdentityCore
// pour remplacer le PasswordHasher<TUser> par defaut (TryAddScoped, donc ecrasable).
builder.Services.AddSingleton<IPasswordHasher<ApplicationUser>, LegacyCompatiblePasswordHasher>();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "KikoleAuth";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    // les deux menent a la meme page : avant, connecte-mais-pas-assez-privilegie et
    // pas-connecte-du-tout redirigeaient deja indifferemment vers Home/ErrorIndex.
    options.LoginPath = "/Home/ErrorIndex";
    options.AccessDeniedPath = "/Home/ErrorIndex";
});

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(
        MinimumUserTypeRequirement.PolicyName(UserTypes.StandardUser),
        p => p.AddRequirements(new MinimumUserTypeRequirement(UserTypes.StandardUser)))
    .AddPolicy(
        MinimumUserTypeRequirement.PolicyName(UserTypes.PowerUser),
        p => p.AddRequirements(new MinimumUserTypeRequirement(UserTypes.PowerUser)))
    .AddPolicy(
        MinimumUserTypeRequirement.PolicyName(UserTypes.Administrator),
        p => p.AddRequirements(new MinimumUserTypeRequirement(UserTypes.Administrator)));

builder.Services.AddSingleton<IAuthorizationHandler, MinimumUserTypeHandler>();

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

var forwardedProxyOptions = app.Services.GetRequiredService<IOptions<ForwardedProxyOptions>>().Value;
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// vides par defaut : ne change rien au comportement natif (boucle locale seule de
// confiance) tant que l'hebergement n'est pas choisi. KnownIPNetworks (System.Net) plutot
// que l'ancien KnownNetworks (Microsoft.AspNetCore.HttpOverrides), qui accepte un CIDR
// directement via IPNetwork.Parse.
foreach (var proxy in forwardedProxyOptions.KnownProxies)
    forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(proxy));
foreach (var network in forwardedProxyOptions.KnownNetworks)
    forwardedHeadersOptions.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));

app.UseForwardedHeaders(forwardedHeadersOptions);

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
