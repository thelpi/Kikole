using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using KikoleSite.Repositories;
using KikoleSite.Services;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Services;

/// <summary>
/// Referentiels partages et leur cache. Ces donnees ne changent qu'a l'initiative d'un
/// administrateur : le service ne doit interroger les depots qu'une fois par langue,
/// et relire les clubs uniquement sur invalidation explicite.
/// </summary>
public class InternationalServiceTests
{
    private readonly Mock<IInternationalRepository> _internationalRepository = new();
    private readonly Mock<IClubRepository> _clubRepository = new();
    private readonly InternationalService _service;

    public InternationalServiceTests()
    {
        _clubRepository.Setup(_ => _.GetClubsAsync()).ReturnsAsync(new List<ClubDto>
        {
            ClubDtoBuilder.Valid().WithId(2).WithName("Real Madrid").Build(),
            ClubDtoBuilder.Valid().WithId(1).WithName("AS Cannes").Build()
        });

        _internationalRepository
            .Setup(_ => _.GetCountriesAsync(It.IsAny<ulong>()))
            .ReturnsAsync(new List<CountryDto>
            {
                CountryDtoBuilder.Valid().WithCode("FR").WithName("Zzz").Build(),
                CountryDtoBuilder.Valid().WithCode("BR").WithName("Aaa").Build()
            });

        _internationalRepository
            .Setup(_ => _.GetContinentsAsync(It.IsAny<ulong>()))
            .ReturnsAsync(new List<ContinentDto>
            {
                ContinentDtoBuilder.Valid().WithId(Continents.Europe).WithName("Zzz").Build(),
                ContinentDtoBuilder.Valid().WithId(Continents.Africa).WithName("Aaa").Build()
            });

        _service = new InternationalService(_internationalRepository.Object, _clubRepository.Object);
    }

    // ------------------------------------------------------------- clubs

    [Fact]
    public async Task GetClubsAsync_OrdersByName()
    {
        var clubs = await _service.GetClubsAsync();

        clubs.Select(c => c.Name).Should().ContainInOrder("AS Cannes", "Real Madrid");
    }

    [Fact]
    public async Task GetClubsAsync_ReadsTheRepositoryOnlyOnce()
    {
        await _service.GetClubsAsync();
        await _service.GetClubsAsync();
        await _service.GetClubsAsync();

        _clubRepository.Verify(_ => _.GetClubsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetClubAsync_FindsTheClubWithoutQueryingAgain()
    {
        await _service.GetClubsAsync();

        var club = await _service.GetClubAsync(2);

        club!.Name.Should().Be("Real Madrid");
        _clubRepository.Verify(_ => _.GetClubsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetClubAsync_WhenTheClubDoesNotExist_ReturnsNull()
    {
        var club = await _service.GetClubAsync(999);

        club.Should().BeNull();
    }

    // ------------------------------------------------------------- ecriture

    [Fact]
    public async Task CreateOrUpdateClubAsync_WithoutIdentifier_Creates()
    {
        await _service.CreateOrUpdateClubAsync(Request(0));

        _clubRepository.Verify(_ => _.CreateClubAsync(It.IsAny<ClubDto>()), Times.Once);
        _clubRepository.Verify(_ => _.UpdateClubAsync(It.IsAny<ClubDto>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdateClubAsync_WithAnIdentifier_Updates()
    {
        await _service.CreateOrUpdateClubAsync(Request(7));

        _clubRepository.Verify(_ => _.UpdateClubAsync(It.Is<ClubDto>(c => c.Id == 7)), Times.Once);
        _clubRepository.Verify(_ => _.CreateClubAsync(It.IsAny<ClubDto>()), Times.Never);
    }

    /// <summary>
    /// L'invalidation n'est plus a la charge de l'appelant : toute ecriture rafraichit
    /// le cache, sans quoi il pourrait devenir obsolete par simple oubli.
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateClubAsync_RefreshesTheCacheItself()
    {
        await _service.GetClubsAsync();

        _clubRepository.Setup(_ => _.GetClubsAsync()).ReturnsAsync(new List<ClubDto>
        {
            ClubDtoBuilder.Valid().WithId(3).WithName("Juventus").Build()
        });

        await _service.CreateOrUpdateClubAsync(Request(0));

        var clubs = await _service.GetClubsAsync();
        clubs.Should().ContainSingle().Which.Name.Should().Be("Juventus");
    }

    [Fact]
    public async Task CreateOrUpdateClubAsync_LeavesTheTranslationsAlone()
    {
        await _service.GetCountriesAsync(Languages.fr);

        await _service.CreateOrUpdateClubAsync(Request(0));
        await _service.GetCountriesAsync(Languages.fr);

        _internationalRepository.Verify(_ => _.GetCountriesAsync(It.IsAny<ulong>()), Times.Once);
    }

    private static ClubRequest Request(ulong id)
    {
        return new ClubRequest
        {
            Id = id,
            Name = "Juventus",
            AllowedNames = new List<string> { "juve" }
        };
    }

    // ------------------------------------------------------------- nationalites

    [Fact]
    public async Task GetCountriesAsync_IndexesByCountryCodeAndOrdersByName()
    {
        var countries = await _service.GetCountriesAsync(Languages.fr);

        countries[(ulong)Countries.FR].Should().Be("Zzz");
        countries.Values.Should().ContainInOrder("Aaa", "Zzz");
    }

    [Fact]
    public async Task GetCountriesAsync_ReadsTheRepositoryOncePerLanguage()
    {
        await _service.GetCountriesAsync(Languages.fr);
        await _service.GetCountriesAsync(Languages.fr);
        await _service.GetCountriesAsync(Languages.en);
        await _service.GetCountriesAsync(Languages.en);

        _internationalRepository.Verify(_ => _.GetCountriesAsync((ulong)Languages.fr), Times.Once);
        _internationalRepository.Verify(_ => _.GetCountriesAsync((ulong)Languages.en), Times.Once);
    }

    // ------------------------------------------------------------- continents

    [Fact]
    public async Task GetContinentsAsync_IndexesByContinentIdAndOrdersByName()
    {
        var continents = await _service.GetContinentsAsync(Languages.fr);

        continents[(ulong)Continents.Europe].Should().Be("Zzz");
        continents.Values.Should().ContainInOrder("Aaa", "Zzz");
    }

    [Fact]
    public async Task GetContinentsAsync_ReadsTheRepositoryOncePerLanguage()
    {
        await _service.GetContinentsAsync(Languages.fr);
        await _service.GetContinentsAsync(Languages.en);
        await _service.GetContinentsAsync(Languages.fr);

        _internationalRepository.Verify(_ => _.GetContinentsAsync((ulong)Languages.fr), Times.Once);
        _internationalRepository.Verify(_ => _.GetContinentsAsync((ulong)Languages.en), Times.Once);
    }

    /// <summary>
    /// L'ancien cache etait indexe sur le code ISO du navigateur alors que le jeu ne
    /// connait que deux langues : chaque langue rencontree creait une entree, toutes
    /// identiques hors francais. La cle est desormais la langue du jeu.
    /// </summary>
    [Fact]
    public async Task TheCacheKeyIsTheGameLanguage_NotTheVisitorCulture()
    {
        await _service.GetCountriesAsync(Languages.en);
        await _service.GetCountriesAsync(Languages.en);

        _internationalRepository.Verify(_ => _.GetCountriesAsync(It.IsAny<ulong>()), Times.Once);
    }
}
