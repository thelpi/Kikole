using System;
using System.Collections.Generic;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSiteUnitTests;

/// <summary>
/// Builders de DTO pour les tests. Chaque builder part d'un objet complet et
/// coherent ; les tests ne surchargent que ce qui les interesse.
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

internal sealed class ClubDtoBuilder
{
    private ClubDto _dto = new()
    {
        Id = 1,
        Name = "Real Madrid",
        CountryId = (ulong)Countries.ESP
    };

    internal static ClubDtoBuilder Valid() => new();

    internal ClubDtoBuilder WithId(ulong id) { _dto = _dto with { Id = id }; return this; }
    internal ClubDtoBuilder WithName(string name) { _dto = _dto with { Name = name }; return this; }
    internal ClubDtoBuilder WithCountryId(ulong countryId) { _dto = _dto with { CountryId = countryId }; return this; }

    internal ClubDto Build() => _dto;
}

internal sealed class ClubTranslationDtoBuilder
{
    private ClubTranslationDto _dto = new()
    {
        ClubId = 1,
        LanguageId = (ulong)Languages.fr,
        Priority = 0,
        Name = "Real Madrid"
    };

    internal static ClubTranslationDtoBuilder Valid() => new();

    internal ClubTranslationDtoBuilder WithClubId(ulong clubId) { _dto = _dto with { ClubId = clubId }; return this; }
    internal ClubTranslationDtoBuilder WithLanguage(Languages language) { _dto = _dto with { LanguageId = (ulong)language }; return this; }
    internal ClubTranslationDtoBuilder WithLanguageId(ulong languageId) { _dto = _dto with { LanguageId = languageId }; return this; }
    internal ClubTranslationDtoBuilder WithPriority(byte priority) { _dto = _dto with { Priority = priority }; return this; }
    internal ClubTranslationDtoBuilder WithName(string name) { _dto = _dto with { Name = name }; return this; }

    internal ClubTranslationDto Build() => _dto;
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

internal sealed class BadgeDtoBuilder
{
    private BadgeDto _dto = new()
    {
        Id = 1,
        Name = "Un badge",
        Description = "Sa description"
    };

    internal static BadgeDtoBuilder Valid() => new();

    internal BadgeDtoBuilder WithId(ulong id) { _dto = _dto with { Id = id }; return this; }
    internal BadgeDtoBuilder WithName(string name) { _dto = _dto with { Name = name }; return this; }
    internal BadgeDtoBuilder WithDescription(string description) { _dto = _dto with { Description = description }; return this; }
    internal BadgeDtoBuilder Hidden(bool hidden = true) { _dto = _dto with { Hidden = (byte)(hidden ? 1 : 0) }; return this; }
    internal BadgeDtoBuilder WithHiddenFlag(byte flag) { _dto = _dto with { Hidden = flag }; return this; }
    internal BadgeDtoBuilder WithCreationDate(DateTime date) { _dto = _dto with { CreationDate = date }; return this; }

    internal BadgeDto Build() => _dto;
}

internal sealed class PlayerFullDtoBuilder
{
    private PlayerDto _player = PlayerDtoBuilder.Valid().Build();
    private IReadOnlyList<ClubDto> _clubs = [];
    private IReadOnlyList<PlayerClubDto> _playerClubs = [];

    internal static PlayerFullDtoBuilder Valid() => new();

    internal PlayerFullDtoBuilder WithPlayer(PlayerDto player) { _player = player; return this; }

    internal PlayerFullDtoBuilder WithCareer(params (ulong clubId, string name, byte position)[] career)
    {
        var clubs = new List<ClubDto>();
        var playerClubs = new List<PlayerClubDto>();
        foreach (var (clubId, name, position) in career)
        {
            if (!clubs.Exists(c => c.Id == clubId))
            {
                clubs.Add(ClubDtoBuilder.Valid()
                    .WithId(clubId)
                    .WithName(name)
                    .Build());
            }
            playerClubs.Add(new PlayerClubDto
            {
                PlayerId = _player.Id,
                ClubId = clubId,
                HistoryPosition = position
            });
        }
        _clubs = clubs;
        _playerClubs = playerClubs;
        return this;
    }

    internal PlayerFullDtoBuilder WithClubs(IReadOnlyList<ClubDto> clubs) { _clubs = clubs; return this; }
    internal PlayerFullDtoBuilder WithPlayerClubs(IReadOnlyList<PlayerClubDto> playerClubs) { _playerClubs = playerClubs; return this; }

    internal PlayerFullDto Build() => new()
    {
        Player = _player,
        Clubs = _clubs,
        PlayerClubs = _playerClubs
    };
}

internal sealed class CountryDtoBuilder
{
    private CountryDto _dto = new() { Code = "FRA", Name = "France" };

    internal static CountryDtoBuilder Valid() => new();

    internal CountryDtoBuilder WithId(ulong id) { _dto = _dto with { Id = id }; return this; }
    internal CountryDtoBuilder WithCode(string code) { _dto = _dto with { Code = code }; return this; }
    internal CountryDtoBuilder WithName(string name) { _dto = _dto with { Name = name }; return this; }
    internal CountryDtoBuilder WithContinentId(ulong id) { _dto = _dto with { ContinentId = id }; return this; }

    internal CountryDto Build() => _dto;
}

/// <summary>
/// Correspondance pays-&gt;continent partagee par les tests qui construisent
/// ProposalResponse/Player directement (le continent n'est plus stocke sur le joueur,
/// il est deduit de cette table).
/// </summary>
internal static class TestCountryContinents
{
    internal static readonly IReadOnlyDictionary<ulong, ulong> Map = new Dictionary<ulong, ulong>
    {
        { (ulong)Countries.FRA, (ulong)Continents.Europe },
        { (ulong)Countries.BRA, (ulong)Continents.SouthAmerica },
        { (ulong)Countries.GDR, (ulong)Continents.Europe },
        { (ulong)Countries.GER, (ulong)Continents.Europe },
        { (ulong)Countries.ITA, (ulong)Continents.Europe },
        { (ulong)Countries.ESP, (ulong)Continents.Europe },
    };
}

internal sealed class ContinentDtoBuilder
{
    private ContinentDto _dto = new() { Name = "Europe" };

    internal static ContinentDtoBuilder Valid() => new();

    internal ContinentDtoBuilder WithId(Continents continent) { _dto = _dto with { Id = (ulong)continent }; return this; }
    internal ContinentDtoBuilder WithName(string name) { _dto = _dto with { Name = name }; return this; }

    internal ContinentDto Build() => _dto;
}

internal sealed class MessageDtoBuilder
{
    private MessageDto _dto = new() { Message = "un message" };

    internal static MessageDtoBuilder Valid() => new();

    internal MessageDtoBuilder WithMessage(string message) { _dto = _dto with { Message = message }; return this; }
    internal MessageDtoBuilder DisplayedFrom(DateTime? from) { _dto = _dto with { DisplayFrom = from }; return this; }
    internal MessageDtoBuilder DisplayedTo(DateTime? to) { _dto = _dto with { DisplayTo = to }; return this; }

    internal MessageDto Build() => _dto;
}
