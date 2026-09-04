using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models;

public class Player : PlayerCreator
{
    public ulong Id { get; }

    public IReadOnlyCollection<PlayerClub> Clubs { get; }

    public ushort YearOfBirth { get; }

    public Continents Continent { get; }

    public Countries Country { get; }

    public Countries? AlternativeCountry { get; }

    public DateTime? PublicationDate { get; }

    public string Clue { get; }

    public string EasyClue { get; }

    public Positions Position { get; }

    public DateTime? RejectDate { get; }

    internal Player(PlayerFullDto p, IEnumerable<UserDto> users)
        : base(Creator(p, users), p.Player)
    {
        Id = p.Player.Id;
        PublicationDate = p.Player.PublicationDate;
        RejectDate = p.Player.RejectDate;
        Clubs = p.PlayerClubs
            .Select(c => new PlayerClub(c, p.Clubs))
            .ToList();
        Clue = p.Player.Clue;
        EasyClue = p.Player.EasyClue;
        Continent = (Continents)p.Player.ContinentId;
        Country = (Countries)p.Player.CountryId;
        AlternativeCountry = p.Player.AlternativeCountryId.HasValue
            ? (Countries)p.Player.AlternativeCountryId.Value
            : null;
        Position = (Positions)p.Player.PositionId;
        YearOfBirth = p.Player.YearOfBirth;
    }

    /// <summary>
    /// Le createur du joueur doit figurer parmi les utilisateurs fournis ; son absence
    /// est une incoherence de donnees, pas un cas a degrader.
    /// </summary>
    private static UserDto Creator(PlayerFullDto p, IEnumerable<UserDto> users)
    {
        return users.SingleOrDefault(u => u.Id == p.Player.CreationUserId)
            ?? throw new InvalidOperationException(
                $"Le createur {p.Player.CreationUserId} du joueur {p.Player.Id} est absent de la liste fournie.");
    }
}
