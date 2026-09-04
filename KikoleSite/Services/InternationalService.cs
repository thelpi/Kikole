using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleSite.Models;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;

namespace KikoleSite.Services;

/// <summary>
/// International service implementation.
/// </summary>
/// <seealso cref="IInternationalService"/>
public class InternationalService : IInternationalService
{
    private readonly IInternationalRepository _internationalRepository;
    private readonly IClubRepository _clubRepository;

    // Les traductions sont indexees sur la langue du jeu, pas sur la culture du visiteur :
    // il n'existe que deux jeux de libelles, et l'ancienne cle — le code ISO du navigateur —
    // creait une entree par langue rencontree, toutes identiques hors francais.
    private readonly ConcurrentDictionary<Languages, IReadOnlyDictionary<ulong, string>> _countries = new();
    private readonly ConcurrentDictionary<Languages, IReadOnlyDictionary<ulong, string>> _continents = new();

    // Une affectation de reference est atomique ; `volatile` garantit en plus que les autres
    // requetes voient la liste completement construite. Deux chargements concurrents restent
    // possibles, sans consequence : ils produisent la meme liste.
    private volatile IReadOnlyCollection<Club>? _clubs;

    // Independant de la langue (ce ne sont que des identifiants), donc pas de dictionnaire
    // par langue comme _countries/_continents.
    private volatile IReadOnlyDictionary<ulong, ulong>? _countryContinents;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="internationalRepository">Instance of <see cref="IInternationalRepository"/>.</param>
    /// <param name="clubRepository">Instance of <see cref="IClubRepository"/>.</param>
    public InternationalService(
        IInternationalRepository internationalRepository,
        IClubRepository clubRepository)
    {
        _internationalRepository = internationalRepository;
        _clubRepository = clubRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Club>> GetClubsAsync()
    {
        var cached = _clubs;
        if (cached != null)
            return cached;

        var clubs = await _clubRepository.GetClubsAsync();
        var translations = await _clubRepository.GetClubTranslationsAsync();
        var translationsByClub = translations.ToLookup(t => t.ClubId);

        var loaded = clubs
            .Select(c => new Club(c, translationsByClub[c.Id]))
            .OrderBy(c => c.Name)
            .ToList();

        _clubs = loaded;
        return loaded;
    }

    /// <inheritdoc />
    public async Task<Club?> GetClubAsync(ulong clubId)
    {
        // le referentiel complet est deja en memoire, et invalide a chaque ecriture :
        // inutile d'aller rechercher une ligne en base
        var clubs = await GetClubsAsync();

        return clubs.SingleOrDefault(c => c.Id == clubId);
    }

    /// <inheritdoc />
    public async Task CreateOrUpdateClubAsync(ClubRequest request)
    {
        // un club sans identifiant est un nouveau club
        var clubId = request.Id;
        if (clubId == 0)
            clubId = await _clubRepository.CreateClubAsync(request.ToDto());
        else
            await _clubRepository.UpdateClubAsync(request.ToDto());

        await _clubRepository.ReplaceClubTranslationsAsync(clubId, request.ToTranslationDtos(clubId));

        InvalidateClubs();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<ulong, string>> GetCountriesAsync(Languages language)
    {
        if (_countries.TryGetValue(language, out var cached))
            return cached;

        var countries = await _internationalRepository.GetCountriesAsync((ulong)language);

        var loaded = countries
            .Select(c => new Country(c))
            .OrderBy(c => c.Name)
            .ToDictionary(c => (ulong)c.Code, c => c.Name);

        return _countries.GetOrAdd(language, loaded);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<ulong, string>> GetContinentsAsync(Languages language)
    {
        if (_continents.TryGetValue(language, out var cached))
            return cached;

        var continents = await _internationalRepository.GetContinentsAsync((ulong)language);

        var loaded = continents
            .Select(c => new Continent(c))
            .OrderBy(c => c.Name)
            .ToDictionary(c => (ulong)c.Id, c => c.Name);

        return _continents.GetOrAdd(language, loaded);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<ulong, ulong>> GetCountryContinentsAsync()
    {
        var cached = _countryContinents;
        if (cached != null)
            return cached;

        var countries = await _internationalRepository.GetCountriesAsync((ulong)Languages.en);

        var loaded = countries.ToDictionary(c => c.Id, c => c.ContinentId);

        _countryContinents = loaded;
        return loaded;
    }

    private void InvalidateClubs()
    {
        _clubs = null;
    }
}
