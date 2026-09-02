using System;
using System.Collections.Generic;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSiteUnitTests
{
    /// <summary>
    /// Builders de DTO pour les tests. Chaque builder part d'un objet complet et
    /// coherent ; les tests ne surchargent que ce qui les interesse.
    /// </summary>
    internal sealed class PlayerDtoBuilder
    {
        private readonly PlayerDto _dto = new PlayerDto
        {
            Id = 1,
            Name = "Zinédine Zidane",
            AllowedNames = "zidane;zizou;zinedine zidane",
            YearOfBirth = 1972,
            CountryId = (ulong)Countries.FR,
            ContinentId = (ulong)Continents.Europe,
            PositionId = (ulong)Positions.Midfielder,
            Clue = "un indice",
            EasyClue = "un indice facile",
            CreationUserId = 42
        };

        internal static PlayerDtoBuilder Valid() => new PlayerDtoBuilder();

        internal PlayerDtoBuilder WithId(ulong id) { _dto.Id = id; return this; }
        internal PlayerDtoBuilder WithName(string name) { _dto.Name = name; return this; }
        internal PlayerDtoBuilder WithAllowedNames(string names) { _dto.AllowedNames = names; return this; }
        internal PlayerDtoBuilder WithYearOfBirth(ushort year) { _dto.YearOfBirth = year; return this; }
        internal PlayerDtoBuilder WithCountry(Countries country) { _dto.CountryId = (ulong)country; return this; }
        internal PlayerDtoBuilder WithContinent(Continents continent) { _dto.ContinentId = (ulong)continent; return this; }
        internal PlayerDtoBuilder WithPosition(Positions position) { _dto.PositionId = (ulong)position; return this; }
        internal PlayerDtoBuilder WithCountryId(ulong id) { _dto.CountryId = id; return this; }
        internal PlayerDtoBuilder WithContinentId(ulong id) { _dto.ContinentId = id; return this; }
        internal PlayerDtoBuilder WithPositionId(ulong id) { _dto.PositionId = id; return this; }
        internal PlayerDtoBuilder WithClue(string clue) { _dto.Clue = clue; return this; }
        internal PlayerDtoBuilder WithEasyClue(string clue) { _dto.EasyClue = clue; return this; }
        internal PlayerDtoBuilder WithHideCreatorFlag(byte flag) { _dto.HideCreator = flag; return this; }
        internal PlayerDtoBuilder WithProposalDate(DateTime? date) { _dto.ProposalDate = date; return this; }
        internal PlayerDtoBuilder WithRejectDate(DateTime? date) { _dto.RejectDate = date; return this; }
        internal PlayerDtoBuilder WithCreator(ulong userId) { _dto.CreationUserId = userId; return this; }
        internal PlayerDtoBuilder WithBadge(ulong? badgeId) { _dto.BadgeId = badgeId; return this; }
        internal PlayerDtoBuilder WithHiddenCreator(bool hidden = true) { _dto.HideCreator = (byte)(hidden ? 1 : 0); return this; }
        internal PlayerDtoBuilder WithClues(string clue, string easyClue) { _dto.Clue = clue; _dto.EasyClue = easyClue; return this; }

        internal PlayerDto Build() => _dto;
    }

    internal sealed class ClubDtoBuilder
    {
        private readonly ClubDto _dto = new ClubDto
        {
            Id = 1,
            Name = "Real Madrid",
            AllowedNames = "real;real madrid"
        };

        internal static ClubDtoBuilder Valid() => new ClubDtoBuilder();

        internal ClubDtoBuilder WithId(ulong id) { _dto.Id = id; return this; }
        internal ClubDtoBuilder WithName(string name) { _dto.Name = name; return this; }
        internal ClubDtoBuilder WithAllowedNames(string names) { _dto.AllowedNames = names; return this; }

        internal ClubDto Build() => _dto;
    }

    internal sealed class UserDtoBuilder
    {
        private readonly UserDto _dto = new UserDto
        {
            Id = 1,
            Login = "joueur",
            Password = "hash",
            PasswordResetQuestion = "une question ?",
            PasswordResetAnswer = "hash-reponse",
            LanguageId = (ulong)Languages.fr,
            UserTypeId = (ulong)UserTypes.StandardUser
        };

        internal static UserDtoBuilder Valid() => new UserDtoBuilder();

        internal UserDtoBuilder WithId(ulong id) { _dto.Id = id; return this; }
        internal UserDtoBuilder WithLogin(string login) { _dto.Login = login; return this; }
        internal UserDtoBuilder WithType(UserTypes type) { _dto.UserTypeId = (ulong)type; return this; }
        internal UserDtoBuilder WithUserTypeId(ulong id) { _dto.UserTypeId = id; return this; }
        internal UserDtoBuilder WithLanguageId(ulong id) { _dto.LanguageId = id; return this; }
        internal UserDtoBuilder WithPasswordResetQuestion(string q) { _dto.PasswordResetQuestion = q; return this; }
        internal UserDtoBuilder WithPasswordResetAnswer(string a) { _dto.PasswordResetAnswer = a; return this; }
        internal UserDtoBuilder WithPassword(string password) { _dto.Password = password; return this; }
        internal UserDtoBuilder WithCreationDate(DateTime date) { _dto.CreationDate = date; return this; }
        internal UserDtoBuilder WithIp(string? ip) { _dto.Ip = ip; return this; }

        internal UserDto Build() => _dto;
    }

    internal sealed class LeaderDtoBuilder
    {
        private readonly LeaderDto _dto = new LeaderDto
        {
            UserId = 1,
            Points = 1000,
            Time = 60
        };

        internal static LeaderDtoBuilder Valid() => new LeaderDtoBuilder();

        internal LeaderDtoBuilder WithUser(ulong userId) { _dto.UserId = userId; return this; }
        internal LeaderDtoBuilder WithUserId(ulong id) { _dto.UserId = id; return this; }
        internal LeaderDtoBuilder WithPoints(ushort points) { _dto.Points = points; return this; }
        internal LeaderDtoBuilder WithTime(int minutes) { _dto.Time = minutes; return this; }
        internal LeaderDtoBuilder WithProposalDate(DateTime date) { _dto.ProposalDate = date; return this; }
        internal LeaderDtoBuilder WithCreationDate(DateTime date) { _dto.CreationDate = date; return this; }

        /// <summary>Trouve le jour meme : la date de creation tombe dans la journee proposee.</summary>
        internal LeaderDtoBuilder OnTheDay(DateTime day, int minutes)
        {
            _dto.ProposalDate = day;
            _dto.Time = minutes;
            _dto.CreationDate = day.AddMinutes(minutes);
            return this;
        }

        /// <summary>Trouve en rattrapage : la date de creation est posterieure au jour propose.</summary>
        internal LeaderDtoBuilder AsCatchUp(DateTime day, int daysLater = 2)
        {
            _dto.ProposalDate = day;
            _dto.CreationDate = day.AddDays(daysLater);
            return this;
        }

        internal LeaderDto Build() => _dto;
    }

    internal sealed class ProposalDtoBuilder
    {
        private readonly ProposalDto _dto = new ProposalDto
        {
            UserId = 1,
            ProposalTypeId = (ulong)ProposalTypes.Name,
            Value = "zidane",
            Successful = 1
        };

        internal static ProposalDtoBuilder Valid() => new ProposalDtoBuilder();

        internal ProposalDtoBuilder WithUser(ulong userId) { _dto.UserId = userId; return this; }
        internal ProposalDtoBuilder OfType(ProposalTypes type) { _dto.ProposalTypeId = (ulong)type; return this; }
        internal ProposalDtoBuilder WithProposalTypeId(ulong id) { _dto.ProposalTypeId = id; return this; }
        internal ProposalDtoBuilder WithSuccessfulFlag(byte flag) { _dto.Successful = flag; return this; }
        internal ProposalDtoBuilder WithValue(string? value) { _dto.Value = value; return this; }
        internal ProposalDtoBuilder Successful(bool successful = true) { _dto.Successful = (byte)(successful ? 1 : 0); return this; }
        internal ProposalDtoBuilder WithProposalDate(DateTime date) { _dto.ProposalDate = date; return this; }
        internal ProposalDtoBuilder WithCreationDate(DateTime date) { _dto.CreationDate = date; return this; }
        internal ProposalDtoBuilder WithIp(string? ip) { _dto.Ip = ip; return this; }

        internal ProposalDto Build() => _dto;
    }

    internal sealed class BadgeDtoBuilder
    {
        private readonly BadgeDto _dto = new BadgeDto
        {
            Id = 1,
            Name = "Un badge",
            Description = "Sa description"
        };

        internal static BadgeDtoBuilder Valid() => new BadgeDtoBuilder();

        internal BadgeDtoBuilder WithId(ulong id) { _dto.Id = id; return this; }
        internal BadgeDtoBuilder WithName(string name) { _dto.Name = name; return this; }
        internal BadgeDtoBuilder WithDescription(string description) { _dto.Description = description; return this; }
        internal BadgeDtoBuilder Hidden(bool hidden = true) { _dto.Hidden = (byte)(hidden ? 1 : 0); return this; }
        internal BadgeDtoBuilder WithHiddenFlag(byte flag) { _dto.Hidden = flag; return this; }
        internal BadgeDtoBuilder WithCreationDate(DateTime date) { _dto.CreationDate = date; return this; }

        internal BadgeDto Build() => _dto;
    }

    internal sealed class PlayerFullDtoBuilder
    {
        private PlayerDto _player = PlayerDtoBuilder.Valid().Build();
        private IReadOnlyList<ClubDto> _clubs = new List<ClubDto>();
        private IReadOnlyList<PlayerClubDto> _playerClubs = new List<PlayerClubDto>();

        internal static PlayerFullDtoBuilder Valid() => new PlayerFullDtoBuilder();

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
                        .WithAllowedNames(name.ToLowerInvariant())
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

        internal PlayerFullDto Build() => new PlayerFullDto
        {
            Player = _player,
            Clubs = _clubs,
            PlayerClubs = _playerClubs
        };
    }

    internal sealed class CountryDtoBuilder
    {
        private readonly CountryDto _dto = new CountryDto { Code = "FR", Name = "France" };

        internal static CountryDtoBuilder Valid() => new CountryDtoBuilder();

        internal CountryDtoBuilder WithCode(string code) { _dto.Code = code; return this; }
        internal CountryDtoBuilder WithName(string name) { _dto.Name = name; return this; }

        internal CountryDto Build() => _dto;
    }

    internal sealed class ContinentDtoBuilder
    {
        private readonly ContinentDto _dto = new ContinentDto { Name = "Europe" };

        internal static ContinentDtoBuilder Valid() => new ContinentDtoBuilder();

        internal ContinentDtoBuilder WithId(Continents continent) { _dto.Id = (ulong)continent; return this; }
        internal ContinentDtoBuilder WithName(string name) { _dto.Name = name; return this; }

        internal ContinentDto Build() => _dto;
    }

    internal sealed class MessageDtoBuilder
    {
        private readonly MessageDto _dto = new MessageDto { Message = "un message" };

        internal static MessageDtoBuilder Valid() => new MessageDtoBuilder();

        internal MessageDtoBuilder WithMessage(string message) { _dto.Message = message; return this; }
        internal MessageDtoBuilder DisplayedFrom(DateTime? from) { _dto.DisplayFrom = from; return this; }
        internal MessageDtoBuilder DisplayedTo(DateTime? to) { _dto.DisplayTo = to; return this; }

        internal MessageDto Build() => _dto;
    }
}
