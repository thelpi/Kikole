using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Xunit;

namespace KikoleSiteUnitTests.Models
{
    /// <summary>
    /// Modeles de presentation construits depuis un DTO. Peu de logique, mais ce sont
    /// eux qui traduisent les identifiants numeriques de la base en valeurs typees.
    /// </summary>
    public class MappingModelsTests
    {
        // ------------------------------------------------------------- Club

        [Fact]
        public void Club_SplitsTheAllowedNamesIntoAList()
        {
            var club = new Club(new ClubDto
            {
                Id = 3,
                Name = "Juventus",
                AllowedNames = "juve;juventus turin;juventus"
            });

            club.Id.Should().Be(3);
            club.Name.Should().Be("Juventus");
            club.AllowedNames.Should().BeEquivalentTo(new[] { "juve", "juventus turin", "juventus" });
        }

        // ------------------------------------------------------------- PlayerClub

        [Fact]
        public void PlayerClub_ResolvesTheClubNameFromTheCareerEntry()
        {
            var clubs = new List<ClubDto>
            {
                new ClubDto { Id = 1, Name = "AS Cannes" },
                new ClubDto { Id = 2, Name = "Real Madrid" }
            };

            var pc = new PlayerClub(
                new PlayerClubDto { ClubId = 2, HistoryPosition = 4, IsLoan = 1 }, clubs);

            pc.Name.Should().Be("Real Madrid");
            pc.HistoryPosition.Should().Be(4);
            pc.IsLoan.Should().BeTrue();
        }

        [Fact]
        public void PlayerClub_WhenTheClubIsMissing_Throws()
        {
            // caracterisation : le Single() n'a aucun garde-fou, une carriere referencant
            // un club absent casse au lieu de degrader
            Action act = () => new PlayerClub(
                new PlayerClubDto { ClubId = 99 }, new List<ClubDto>());

            act.Should().Throw<InvalidOperationException>();
        }

        // ------------------------------------------------------------- Country / Continent

        [Fact]
        public void Country_ParsesTheIsoCodeIntoItsEnum()
        {
            var country = new Country(new CountryDto { Code = "FR", Name = "France" });

            country.Code.Should().Be(Countries.FR);
            country.Name.Should().Be("France");
        }

        [Fact]
        public void Country_WhenTheCodeIsUnknown_Throws()
        {
            Action act = () => new Country(new CountryDto { Code = "ZZZ", Name = "Nulle part" });

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Continent_MapsTheIdentifierOntoItsEnum()
        {
            var continent = new Continent(new ContinentDto
            {
                Id = (ulong)Continents.SouthAmerica,
                Name = "Amérique du Sud"
            });

            continent.Id.Should().Be(Continents.SouthAmerica);
            continent.Name.Should().Be("Amérique du Sud");
        }

        // ------------------------------------------------------------- User / Badge

        [Fact]
        public void User_KeepsOnlyTheIdentityFieldsAndDropsTheCredentials()
        {
            var user = new User(new UserDto
            {
                Id = 7,
                Login = "joueur",
                Password = "un-hash-secret",
                PasswordResetAnswer = "un-autre-hash"
            });

            user.Id.Should().Be(7);
            user.Login.Should().Be("joueur");
            user.GetType().GetProperties().Select(p => p.Name)
                .Should().BeEquivalentTo(new[] { "Id", "Login" });
        }

        [Theory]
        [InlineData((byte)0, false)]
        [InlineData((byte)1, true)]
        [InlineData((byte)2, true)]
        public void Badge_TreatsAnyNonZeroHiddenFlagAsHidden(byte hidden, bool expected)
        {
            var badge = new Badge(
                new BadgeDto { Id = 1, Name = "Un badge", Description = "en", Hidden = hidden },
                12, null);

            badge.Hidden.Should().Be(expected);
            badge.Users.Should().Be(12);
        }

        [Fact]
        public void Badge_UsesTheTranslationWhenThereIsOne()
        {
            var dto = new BadgeDto { Id = 1, Name = "Un badge", Description = "english description" };

            new Badge(dto, 1, "description française").Description.Should().Be("description française");
            new Badge(dto, 1, null).Description.Should().Be("english description");
        }

        [Fact]
        public void UserBadge_ForwardsTheBadgeAndStampsTheDate()
        {
            var badge = new Badge(
                new BadgeDto { Id = 5, Name = "Wooden spoon", Description = "d", Hidden = 1 }, 3, null);
            var date = new DateTime(2026, 9, 2);

            var userBadge = new UserBadge(badge, date);

            userBadge.Id.Should().Be(5);
            userBadge.Name.Should().Be("Wooden spoon");
            userBadge.Users.Should().Be(3);
            userBadge.Hidden.Should().BeTrue();
            userBadge.GetDate.Should().Be(date);
        }

        // ------------------------------------------------------------- Player

        private static PlayerFullDto Submission(ulong creatorId)
        {
            return new PlayerFullDto
            {
                Player = new PlayerDto
                {
                    Id = 1,
                    Name = "Zinédine Zidane",
                    AllowedNames = "zidane;zizou",
                    YearOfBirth = 1972,
                    CountryId = (ulong)Countries.FR,
                    ContinentId = (ulong)Continents.Europe,
                    PositionId = (ulong)Positions.Midfielder,
                    CreationUserId = creatorId,
                    Clue = "un indice",
                    EasyClue = "un indice facile"
                },
                Clubs = new List<ClubDto> { new ClubDto { Id = 2, Name = "Real Madrid" } },
                PlayerClubs = new List<PlayerClubDto>
                {
                    new PlayerClubDto { PlayerId = 1, ClubId = 2, HistoryPosition = 4 }
                }
            };
        }

        [Fact]
        public void Player_MapsEveryIdentifierOntoItsEnum()
        {
            var users = new List<UserDto> { new UserDto { Id = 42, Login = "createur" } };

            var player = new Player(Submission(42), users);

            player.Id.Should().Be(1);
            player.Country.Should().Be(Countries.FR);
            player.Continent.Should().Be(Continents.Europe);
            player.Position.Should().Be(Positions.Midfielder);
            player.YearOfBirth.Should().Be(1972);
            player.Clubs.Should().ContainSingle().Which.Name.Should().Be("Real Madrid");
        }

        [Fact]
        public void Player_AlwaysExposesTheAnswerToTheAdministrationScreen()
        {
            // ce constructeur sert l'ecran de validation des soumissions : contrairement
            // a PlayerCreator, il ne masque rien
            var users = new List<UserDto> { new UserDto { Id = 42, Login = "createur" } };

            var player = new Player(Submission(42), users);

            player.Name.Should().Be("Zinédine Zidane");
            player.AllowedNames.Should().BeEquivalentTo(new[] { "zidane", "zizou" });
            player.Login.Should().Be("createur");
        }

        [Fact]
        public void Player_WhenTheCreatorIsAbsentFromTheList_Throws()
        {
            Action act = () => new Player(Submission(99), new List<UserDto>());

            act.Should().Throw<InvalidOperationException>();
        }
    }

    /// <summary>
    /// Horloge applicative. Les proprietes derivent toutes de l'heure systeme : on
    /// verifie leurs relations entre elles, pas leur valeur absolue.
    /// </summary>
    public class ClockTests
    {
        private readonly IClock _clock = new Clock();

        [Fact]
        public void TodayHasNoTimeComponent()
        {
            _clock.Today.Should().Be(_clock.Today.Date);
        }

        [Fact]
        public void TomorrowAndYesterdayFrameToday()
        {
            _clock.Tomorrow.Should().Be(_clock.Today.AddDays(1));
            _clock.Yesterday.Should().Be(_clock.Today.AddDays(-1));
        }

        [Fact]
        public void TomorrowEndIsTheLastSecondOfTomorrow()
        {
            _clock.TomorrowEnd.Should().Be(_clock.Tomorrow.AddDays(1).AddSeconds(-1));
            _clock.TomorrowEnd.TimeOfDay.Should().Be(new TimeSpan(23, 59, 59));
        }

        [Fact]
        public void FirstOfMonthIsTheFirstDayOfTheCurrentMonth()
        {
            _clock.FirstOfMonth.Day.Should().Be(1);
            _clock.FirstOfMonth.Month.Should().Be(_clock.Today.Month);
            _clock.FirstOfMonth.Year.Should().Be(_clock.Today.Year);
        }

        [Fact]
        public void NowSecondsDropsTheMilliseconds()
        {
            _clock.NowSeconds.Millisecond.Should().Be(0);
        }

        [Fact]
        public void IsTomorrowIn_IsFalseForZeroMinutesAndTrueForAWholeDay()
        {
            // garde-fou de ReassignPlayersOfTheDayAsync : ajouter 24 h franchit
            // forcement minuit, ajouter zero minute ne le franchit jamais
            _clock.IsTomorrowIn(0).Should().BeFalse();
            _clock.IsTomorrowIn(24 * 60).Should().BeTrue();
        }

        [Fact]
        public void IsTomorrowIn_IsMonotonic()
        {
            // si un delai court franchit minuit, un delai plus long le franchit aussi
            if (_clock.IsTomorrowIn(30))
                _clock.IsTomorrowIn(60).Should().BeTrue();
        }
    }
}
