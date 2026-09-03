using System.Collections.Generic;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;

namespace KikoleSiteUnitTests;

/// <summary>
/// Builders des requetes applicatives. Depuis le passage aux membres <c>required</c>,
/// une requete ne peut plus etre construite partiellement : ces builders portent le
/// minimum valable, chaque test ne surchargeant que ce qu'il met a l'epreuve.
/// </summary>
internal sealed class UserRequestBuilder
{
    private UserRequest _request;

    private UserRequestBuilder(UserRequest request)
    {
        _request = request;
    }

    /// <summary>
    /// Le strict necessaire d'une inscription : identifiants seuls, sans question de
    /// recuperation ni adresse IP, qui sont facultatives cote formulaire.
    /// </summary>
    internal static UserRequestBuilder Valid()
    {
        return new UserRequestBuilder(new UserRequest
        {
            Login = "joueur",
            Password = "p",
            PasswordResetQuestion = null,
            PasswordResetAnswer = null,
            Ip = null
        });
    }

    internal UserRequestBuilder WithLogin(string login) { _request = _request with { Login = login }; return this; }

    internal UserRequestBuilder WithPassword(string password) { _request = _request with { Password = password }; return this; }

    internal UserRequestBuilder WithRecovery(string? question, string? answer)
    {
        _request = _request with { PasswordResetQuestion = question, PasswordResetAnswer = answer };
        return this;
    }

    internal UserRequestBuilder WithLanguage(Languages? language) { _request = _request with { Language = language }; return this; }

    internal UserRequestBuilder WithIp(string? ip) { _request = _request with { Ip = ip }; return this; }

    internal UserRequest Build() => _request;
}

internal sealed class PlayerSubmissionValidationRequestBuilder
{
    private PlayerSubmissionValidationRequest _request;

    private PlayerSubmissionValidationRequestBuilder(PlayerSubmissionValidationRequest request)
    {
        _request = request;
    }

    /// <summary>
    /// Une validation vierge : ni acceptation, ni refus motive, ni indice reecrit.
    /// </summary>
    internal static PlayerSubmissionValidationRequestBuilder Valid()
    {
        return new PlayerSubmissionValidationRequestBuilder(new PlayerSubmissionValidationRequest
        {
            PlayerId = 1,
            IsAccepted = false,
            ClueEditLanguages = new Dictionary<Languages, string?>(),
            EasyClueEditLanguages = new Dictionary<Languages, string?>(),
            ClueEditEn = null,
            EasyClueEditEn = null,
            RefusalReason = null
        });
    }

    internal PlayerSubmissionValidationRequestBuilder WithPlayerId(ulong playerId)
    {
        _request = _request with { PlayerId = playerId };
        return this;
    }

    /// <summary>
    /// Acceptation, avec les indices francais que la validation exige.
    /// </summary>
    internal PlayerSubmissionValidationRequestBuilder Accepted(
        string? clueFr = "indice", string? easyClueFr = "indice facile")
    {
        _request = _request with { IsAccepted = true };
        _request = _request with { ClueEditLanguages = new Dictionary<Languages, string?> { { Languages.fr, clueFr } } };
        _request = _request with { EasyClueEditLanguages = new Dictionary<Languages, string?> { { Languages.fr, easyClueFr } } };
        return this;
    }

    internal PlayerSubmissionValidationRequestBuilder Refused(string? reason)
    {
        _request = _request with { IsAccepted = false, RefusalReason = reason };
        return this;
    }

    internal PlayerSubmissionValidationRequestBuilder WithClueEditLanguages(
        IReadOnlyDictionary<Languages, string?> clues)
    {
        _request = _request with { ClueEditLanguages = clues };
        return this;
    }

    internal PlayerSubmissionValidationRequestBuilder WithEasyClueEditLanguages(
        IReadOnlyDictionary<Languages, string?> clues)
    {
        _request = _request with { EasyClueEditLanguages = clues };
        return this;
    }

    internal PlayerSubmissionValidationRequestBuilder WithEnglishClues(string? clue, string? easyClue)
    {
        _request = _request with { ClueEditEn = clue, EasyClueEditEn = easyClue };
        return this;
    }

    internal PlayerSubmissionValidationRequest Build() => _request;
}
