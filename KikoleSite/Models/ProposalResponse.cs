using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Helpers;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models;

public class ProposalResponse
{
    private readonly List<UserBadge> _badges = [];

    public bool Successful { get; }

    public object? Value { get; }

    public string? RawValue { get; }

    /// <summary>
    /// Renseigne uniquement pour une proposition <see cref="ProposalTypes.Country"/>
    /// reussie sur un joueur ayant une nation sportive disparue en plus de la
    /// principale (<see cref="PlayerDto.AlternativeCountryId"/>) - les deux sont
    /// alors affichees au reveal, peu importe laquelle a ete devinee.
    /// </summary>
    public ulong? AlternativeCountryId { get; }

    public DateTime Date { get; }

    public string? Tip { get; }

    /// <summary>Le tarif de la catégorie (montant ou taux, cf. <see cref="ScoreCalculator.ProposalTypesCost"/>).</summary>
    public (int, bool) Cost { get; }

    public ProposalTypes ProposalType { get; }

    public int TotalPoints { get; private set; }

    /// <summary>
    /// Ce que cette proposition a réellement coûté, une fois <see cref="TotalPoints"/>
    /// plafonné à 0 pris en compte — peut être inférieur au tarif de <see cref="Cost"/>
    /// si le score restant ne suffisait pas à l'absorber.
    /// </summary>
    public int PointsLost { get; private set; }

    public IReadOnlyCollection<UserBadge> CollectedBadges => _badges;

    internal bool IsWin => ProposalType == ProposalTypes.Name && Successful;

    private ProposalResponse(ProposalTypes proposalType,
        string? sourceValue,
        bool? success,
        PlayerFullDto player)
    {
        ProposalType = proposalType;

        if (success.HasValue)
            Successful = success.Value;

        RawValue = sourceValue;

        // seuls l'achat d'indice et l'achat de classement n'ont pas de valeur : partout
        // ailleurs elle a ete validee par le controleur avant d'arriver ici, et son
        // absence signale une ligne de proposition incoherente en base
        string Guessed() => sourceValue
            ?? throw new InvalidOperationException($"Une proposition de type {proposalType} doit porter une valeur.");

        switch (ProposalType)
        {
            case ProposalTypes.Name:
                if (!success.HasValue)
                    Successful = player.Player.AllowedNames.ContainsApproximately(Guessed());
                Value = Successful
                    ? player.Player.Name
                    : sourceValue;
                break;

            case ProposalTypes.Club:
                var c = ulong.TryParse(Guessed(), out var clubId)
                    ? player.Clubs.FirstOrDefault(_ => _.Id == clubId)
                    : null;
                if (!success.HasValue)
                    Successful = c != null;
                if (Successful)
                {
                    if (c == null)
                    {
                        // weird case from the beginning when there was no autocompletion on clubs
                        // the value is not really used in that case anyway
                        // it just need to be not null
                        Value = new List<PlayerClub>();
                    }
                    else
                    {
                        Value = player.PlayerClubs
                            .Where(_ => _.ClubId == c.Id)
                            .Select(_ => new PlayerClub(_, player.Clubs))
                            .ToList();
                    }
                }
                else
                    Value = sourceValue;
                break;

            case ProposalTypes.Country:
                if (!success.HasValue)
                {
                    var guessedCountryId = (ulong)Enum.Parse<Countries>(Guessed());
                    Successful = player.Player.CountryId == guessedCountryId
                        || player.Player.AlternativeCountryId == guessedCountryId;
                }
                Value = Successful
                    ? player.Player.CountryId
                    : sourceValue;
                if (Successful)
                    AlternativeCountryId = player.Player.AlternativeCountryId;
                RawValue = Enum.TryParse<Countries>(sourceValue, out var tmpRawCountry)
                    ? tmpRawCountry.ToString()
                    : RawValue;
                break;

            case ProposalTypes.Continent:
                if (!success.HasValue)
                    Successful = player.Player.ContinentId == (ulong)Enum.Parse<Continents>(Guessed());
                Value = Successful
                    ? player.Player.ContinentId
                    : sourceValue;
                RawValue = Enum.TryParse<Continents>(sourceValue, out var tmpRawContinent)
                    ? tmpRawContinent.ToString()
                    : RawValue;
                break;

            case ProposalTypes.Position:
                if (!success.HasValue)
                    Successful = player.Player.PositionId == ulong.Parse(Guessed());
                Value = Successful
                    ? player.Player.PositionId
                    : sourceValue;
                RawValue = Enum.TryParse<Positions>(sourceValue, out var tmpRawPosition)
                    ? tmpRawPosition.ToString()
                    : RawValue;
                break;

            case ProposalTypes.Year:
                if (!success.HasValue)
                    Successful = ushort.Parse(Guessed()) == player.Player.YearOfBirth;
                Value = Successful
                    ? player.Player.YearOfBirth
                    : sourceValue;
                break;

            case ProposalTypes.Leaderboard:
            case ProposalTypes.Clue:
                if (!success.HasValue)
                    Successful = true;
                Value = null;
                RawValue = string.Empty;
                break;
        }

        if (Successful && ProposalType.CanBeMiss())
            Cost = (0, false);
        else
            Cost = ScoreCalculator.ProposalTypesCost[ProposalType];
    }

    internal ProposalResponse(ProposalRequest request, PlayerFullDto player, IStringLocalizer resources)
        : this(request.ProposalType, request.Value, null, player)
    {
        Date = request.ProposalDateTime;
        Tip = request.GetTip(player.Player, resources);
    }

    internal ProposalResponse(ProposalDto dto, PlayerFullDto player, IStringLocalizer resources)
        : this((ProposalTypes)dto.ProposalTypeId, dto.Value, dto.Successful > 0, player)
    {
        // a bit ugly, ngl
        if ((ProposalTypes)dto.ProposalTypeId == ProposalTypes.Year)
        {
            Tip = Convert.ToUInt16(Value) > player.Player.YearOfBirth
                ? resources["TipOlderPlayerShort"]
                : resources["TipYoungerPlayerShort"];
        }
        Date = dto.CreationDate;
    }

    internal ProposalResponse WithTotalPoints(int sourcePoints, bool duplicate)
    {
        var lostPoints = 0;
        if (!duplicate)
        {
            lostPoints = Cost.Item2
                ? (int)Math.Round(sourcePoints * Cost.Item1 / (decimal)100)
                : Cost.Item1;
        }
        TotalPoints = Math.Max(0, sourcePoints - lostPoints);
        PointsLost = sourcePoints - TotalPoints;
        return this;
    }

    internal void AddBadge(UserBadge badge)
    {
        _badges.Add(badge);
    }
}
