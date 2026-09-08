using System;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSiteIntegrationTests;

/// <summary>
/// Builders de DTO pour les tests. Chaque builder part d'un objet complet et
/// coherent ; les tests ne surchargent que ce qui les interesse.
/// Copie volontairement restreinte aux builders utilises par les tests d'integration
/// (le projet KikoleSiteUnitTests a le sien, plus complet, pour les tests unitaires -
/// separation complete entre les deux projets, cf. TODO).
/// </summary>
internal sealed class PlayerDtoBuilder
{
    private PlayerDto _dto = new()
    {
        Id = 1,
        Name = "Zinédine Zidane",
        AllowedNames = "zidane;zizou;zinedine zidane",
        YearOfBirth = 1972,
        CountryId = (ulong)Countries.FRA,
        PositionId = (ulong)Positions.Midfielder,
        Clue = "un indice",
        EasyClue = "un indice facile",
        CreationUserId = 42
    };

    internal static PlayerDtoBuilder Valid() => new();

    internal PlayerDtoBuilder WithId(ulong id) { _dto = _dto with { Id = id }; return this; }
    internal PlayerDtoBuilder WithName(string name) { _dto = _dto with { Name = name }; return this; }
    internal PlayerDtoBuilder WithAllowedNames(string names) { _dto = _dto with { AllowedNames = names }; return this; }
    internal PlayerDtoBuilder WithYearOfBirth(ushort year) { _dto = _dto with { YearOfBirth = year }; return this; }
    internal PlayerDtoBuilder WithCountry(Countries country) { _dto = _dto with { CountryId = (ulong)country }; return this; }
    internal PlayerDtoBuilder WithPosition(Positions position) { _dto = _dto with { PositionId = (ulong)position }; return this; }
    internal PlayerDtoBuilder WithCountryId(ulong id) { _dto = _dto with { CountryId = id }; return this; }
    internal PlayerDtoBuilder WithAlternativeCountryId(ulong? id) { _dto = _dto with { AlternativeCountryId = id }; return this; }
    internal PlayerDtoBuilder WithPositionId(ulong id) { _dto = _dto with { PositionId = id }; return this; }
    internal PlayerDtoBuilder WithAlternativePositionId(ulong? id) { _dto = _dto with { AlternativePositionId = id }; return this; }
    internal PlayerDtoBuilder WithClue(string clue) { _dto = _dto with { Clue = clue }; return this; }
    internal PlayerDtoBuilder WithEasyClue(string clue) { _dto = _dto with { EasyClue = clue }; return this; }
    internal PlayerDtoBuilder WithHideCreatorFlag(byte flag) { _dto = _dto with { HideCreator = flag }; return this; }
    internal PlayerDtoBuilder WithPublicationDate(DateTime? date) { _dto = _dto with { PublicationDate = date }; return this; }
    internal PlayerDtoBuilder WithRejectDate(DateTime? date) { _dto = _dto with { RejectDate = date }; return this; }
    internal PlayerDtoBuilder WithCreator(ulong userId) { _dto = _dto with { CreationUserId = userId }; return this; }
    internal PlayerDtoBuilder WithBadge(ulong? badgeId) { _dto = _dto with { BadgeId = badgeId }; return this; }
    internal PlayerDtoBuilder WithHiddenCreator(bool hidden = true) { _dto = _dto with { HideCreator = (byte)(hidden ? 1 : 0) }; return this; }
    internal PlayerDtoBuilder WithClues(string clue, string easyClue) { _dto = _dto with { Clue = clue }; _dto = _dto with { EasyClue = easyClue }; return this; }

    internal PlayerDto Build() => _dto;
}

internal sealed class UserDtoBuilder
{
    private UserDto _dto = new()
    {
        Id = 1,
        Login = "joueur",
        NormalizedLogin = "JOUEUR",
        Password = "hash",
        PasswordResetQuestion = "une question ?",
        PasswordResetAnswer = "hash-reponse",
        LanguageId = (ulong)Languages.fr,
        UserTypeId = (ulong)UserTypes.StandardUser,
        ConcurrencyStamp = "concurrency-stamp",
        SecurityStamp = "security-stamp",
        LockoutEnabled = true
    };

    internal static UserDtoBuilder Valid() => new();

    internal UserDtoBuilder WithId(ulong id) { _dto = _dto with { Id = id }; return this; }
    internal UserDtoBuilder WithLogin(string login) { _dto = _dto with { Login = login, NormalizedLogin = login.ToUpperInvariant() }; return this; }
    internal UserDtoBuilder WithType(UserTypes type) { _dto = _dto with { UserTypeId = (ulong)type }; return this; }
    internal UserDtoBuilder WithUserTypeId(ulong id) { _dto = _dto with { UserTypeId = id }; return this; }
    internal UserDtoBuilder WithLanguageId(ulong id) { _dto = _dto with { LanguageId = id }; return this; }
    internal UserDtoBuilder WithPasswordResetQuestion(string q) { _dto = _dto with { PasswordResetQuestion = q }; return this; }
    internal UserDtoBuilder WithPasswordResetAnswer(string a) { _dto = _dto with { PasswordResetAnswer = a }; return this; }
    internal UserDtoBuilder WithPassword(string password) { _dto = _dto with { Password = password }; return this; }
    internal UserDtoBuilder WithCreationDate(DateTime date) { _dto = _dto with { CreationDate = date }; return this; }
    internal UserDtoBuilder WithIp(string? ip) { _dto = _dto with { Ip = ip }; return this; }
    internal UserDtoBuilder WithDisabled(bool disabled = true) { _dto = _dto with { IsDisabled = disabled }; return this; }

    internal UserDto Build() => _dto;
}

internal sealed class LeaderDtoBuilder
{
    private LeaderDto _dto = new()
    {
        UserId = 1,
        Points = 1000,
        Time = 60
    };

    internal static LeaderDtoBuilder Valid() => new();

    internal LeaderDtoBuilder WithUser(ulong userId) { _dto = _dto with { UserId = userId }; return this; }
    internal LeaderDtoBuilder WithUserId(ulong id) { _dto = _dto with { UserId = id }; return this; }
    internal LeaderDtoBuilder WithPoints(ushort points) { _dto = _dto with { Points = points }; return this; }
    internal LeaderDtoBuilder WithTime(int minutes) { _dto = _dto with { Time = minutes }; return this; }
    internal LeaderDtoBuilder WithProposalDate(DateTime date) { _dto = _dto with { ProposalDate = date }; return this; }
    internal LeaderDtoBuilder WithCreationDate(DateTime date) { _dto = _dto with { CreationDate = date }; return this; }

    /// <summary>Trouve le jour meme : la date de creation tombe dans la journee proposee.</summary>
    internal LeaderDtoBuilder OnTheDay(DateTime day, int minutes)
    {
        _dto = _dto with { ProposalDate = day, Time = minutes, CreationDate = day.AddMinutes(minutes) };
        return this;
    }

    /// <summary>Trouve en rattrapage : la date de creation est posterieure au jour propose.</summary>
    internal LeaderDtoBuilder AsCatchUp(DateTime day, int daysLater = 2)
    {
        _dto = _dto with { ProposalDate = day, CreationDate = day.AddDays(daysLater) };
        return this;
    }

    internal LeaderDto Build() => _dto;
}

internal sealed class ProposalDtoBuilder
{
    private ProposalDto _dto = new()
    {
        UserId = 1,
        ProposalTypeId = (ulong)ProposalTypes.Name,
        Value = "zidane",
        Successful = 1
    };

    internal static ProposalDtoBuilder Valid() => new();

    internal ProposalDtoBuilder WithUser(ulong userId) { _dto = _dto with { UserId = userId }; return this; }
    internal ProposalDtoBuilder OfType(ProposalTypes type) { _dto = _dto with { ProposalTypeId = (ulong)type }; return this; }
    internal ProposalDtoBuilder WithProposalTypeId(ulong id) { _dto = _dto with { ProposalTypeId = id }; return this; }
    internal ProposalDtoBuilder WithSuccessfulFlag(byte flag) { _dto = _dto with { Successful = flag }; return this; }
    internal ProposalDtoBuilder WithValue(string? value) { _dto = _dto with { Value = value }; return this; }
    internal ProposalDtoBuilder Successful(bool successful = true) { _dto = _dto with { Successful = (byte)(successful ? 1 : 0) }; return this; }
    internal ProposalDtoBuilder WithProposalDate(DateTime date) { _dto = _dto with { ProposalDate = date }; return this; }
    internal ProposalDtoBuilder WithCreationDate(DateTime date) { _dto = _dto with { CreationDate = date }; return this; }
    internal ProposalDtoBuilder WithIp(string? ip) { _dto = _dto with { Ip = ip }; return this; }

    internal ProposalDto Build() => _dto;
}
