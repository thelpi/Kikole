using System;
using System.Collections.Generic;
using FluentAssertions;
using KikoleSite;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Models.Requests;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Models.Requests
{
    public class ClubRequestTests
    {
        private readonly IStringLocalizer _localizer = Localizer.Echo();

        private static ClubRequest Valid()
        {
            return new ClubRequest
            {
                Id = 3,
                Name = "Bayern München",
                AllowedNames = new List<string> { "Bayern", "Bayern Munich" }
            };
        }

        [Fact]
        public void IsValid_WhenEverythingIsFilled_ReturnsNull()
        {
            Valid().IsValid(_localizer).Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("  ")]
        public void IsValid_WhenNameIsBlank_IsRejected(string name)
        {
            var request = Valid();
            request.Name = name;

            request.IsValid(_localizer).Should().Be("InvalidName");
        }

        [Fact]
        public void IsValid_WhenAllowedNamesIsEmpty_IsRejected()
        {
            var request = Valid();
            request.AllowedNames = new List<string>();

            request.IsValid(_localizer).Should().Be("InvalidAllowedNames");
        }

        [Fact]
        public void ToDto_SanitizesAliasesAndAppendsTheDisplayName()
        {
            var dto = Valid().ToDto();

            dto.AllowedNames.Should().Be("bayern;bayern munich;bayern munchen");
            dto.Name.Should().Be("Bayern München");
            dto.Id.Should().Be(3);
        }

        [Fact]
        public void ToDto_WhenAnAliasEqualsTheNameOnceSanitized_ItIsNotDuplicated()
        {
            var request = Valid();
            request.AllowedNames = new List<string> { "Bayern Munchen" };

            request.ToDto().AllowedNames.Should().Be("bayern munchen");
        }
    }

    public class PlayerClubRequestTests
    {
        [Theory]
        [InlineData(true, (byte)1)]
        [InlineData(false, (byte)0)]
        public void ToPlayerClubDto_ConvertsTheLoanFlag(bool isLoan, byte expected)
        {
            var dto = new PlayerClubRequest { ClubId = 5, HistoryPosition = 2, IsLoan = isLoan }
                .ToPlayerClubDto(9);

            dto.IsLoan.Should().Be(expected);
            dto.ClubId.Should().Be(5);
            dto.HistoryPosition.Should().Be(2);
            dto.PlayerId.Should().Be(9);
        }
    }

    public class PlayerSubmissionValidationRequestTests
    {
        private readonly IStringLocalizer _localizer = Localizer.Echo();

        private static PlayerSubmissionValidationRequest Accepted()
        {
            return PlayerSubmissionValidationRequestBuilder.Valid().Accepted().Build();
        }

        [Fact]
        public void IsValid_WhenAcceptedWithFrenchClues_ReturnsNull()
        {
            Accepted().IsValid(_localizer).Should().BeNull();
        }

        [Fact]
        public void IsValid_WhenPlayerIdIsMissing_IsRejected()
        {
            var request = Accepted();
            request.PlayerId = 0;

            request.IsValid(_localizer).Should().Be("InvalidPlayerId");
        }

        [Fact]
        public void IsValid_WhenRefusedWithoutReason_IsRejected()
        {
            var request = PlayerSubmissionValidationRequestBuilder.Valid().Build();

            request.IsValid(_localizer).Should().Be("RefusalWithoutReason");
        }

        [Fact]
        public void IsValid_WhenRefusedWithAReason_CluesAreNotRequired()
        {
            var request = PlayerSubmissionValidationRequestBuilder.Valid().Refused("joueur deja utilise").Build();

            request.IsValid(_localizer).Should().BeNull();
        }

        [Fact]
        public void IsValid_WhenAcceptedWithoutFrenchClue_IsRejected()
        {
            var request = Accepted();
            request.ClueEditLanguages = new Dictionary<Languages, string?>();

            request.IsValid(_localizer).Should().Be("InvalidClue");
        }

        [Fact]
        public void IsValid_WhenAcceptedWithABlankClue_IsRejected()
        {
            var request = Accepted();
            request.ClueEditLanguages = new Dictionary<Languages, string?> { { Languages.fr, "   " } };

            request.IsValid(_localizer).Should().Be("InvalidClue");
        }

        [Fact]
        public void IsValid_WhenAcceptedWithoutEasyFrenchClue_IsRejected()
        {
            var request = Accepted();
            request.EasyClueEditLanguages = new Dictionary<Languages, string?>();

            request.IsValid(_localizer).Should().Be("InvalidClue");
        }
    }

    public class UserRequestTests
    {
        private static Mock<ICrypter> Crypter()
        {
            var mock = new Mock<ICrypter>();
            mock.Setup(_ => _.Encrypt(It.IsAny<string>())).Returns<string>(v => "hash:" + v);
            mock.Setup(_ => _.Generate()).Returns("GENERATED");
            return mock;
        }

        [Fact]
        public void ToDto_SanitizesTheLogin()
        {
            // le login est stocke sanitise, et la recherche l'est aussi : c'est ce qui
            // rend la connexion insensible a la casse et aux accents
            var dto = UserRequestBuilder.Valid().WithLogin("  Réné  ").Build().ToDto(Crypter().Object);

            dto.Login.Should().Be("rene");
        }

        [Fact]
        public void ToDto_HashesThePasswordAndTheRecoveryAnswer()
        {
            var dto = UserRequestBuilder.Valid()
                .WithPassword("secret")
                .WithRecovery("Ma question ?", "Ma Réponse")
                .Build()
                .ToDto(Crypter().Object);

            dto.Password.Should().Be("hash:secret");
            // la reponse est sanitisee avant hachage, la question ne l'est pas
            dto.PasswordResetAnswer.Should().Be("hash:ma reponse");
            dto.PasswordResetQuestion.Should().Be("Ma question ?");
        }

        [Fact]
        public void ToDto_WhenRecoveryIsNotProvided_GeneratesUnguessableValues()
        {
            var crypter = Crypter();

            var dto = UserRequestBuilder.Valid().Build().ToDto(crypter.Object);

            dto.PasswordResetQuestion.Should().Be("GENERATED");
            dto.PasswordResetAnswer.Should().Be("hash:GENERATED");
            crypter.Verify(_ => _.Generate(), Times.Exactly(2));
        }

        [Fact]
        public void ToDto_DefaultsToEnglishAndStandardUser()
        {
            var dto = UserRequestBuilder.Valid().Build().ToDto(Crypter().Object);

            dto.LanguageId.Should().Be((ulong)Languages.en);
            dto.UserTypeId.Should().Be((ulong)UserTypes.StandardUser);
        }

        [Fact]
        public void ToDto_NeverGrantsAdministratorRights()
        {
            // garde-fou : la creation de compte ne doit jamais pouvoir produire un admin
            var dto = UserRequestBuilder.Valid().WithLanguage(Languages.fr).Build()
                .ToDto(Crypter().Object);

            dto.UserTypeId.Should().NotBe((ulong)UserTypes.Administrator);
            dto.LanguageId.Should().Be((ulong)Languages.fr);
        }

        [Fact]
        public void ToDto_CarriesTheIpThrough()
        {
            var dto = UserRequestBuilder.Valid().WithIp("::1").Build()
                .ToDto(Crypter().Object);

            dto.Ip.Should().Be("::1");
        }
    }

    public class ProposalRequestTests
    {
        [Theory]
        [InlineData(0u, true)]
        [InlineData(1u, false)]
        [InlineData(30u, false)]
        public void IsTodayPlayer_IsOnlyTrueForTheCurrentDay(uint daysBeforeNow, bool expected)
        {
            new ProposalRequest { DaysBeforeNow = daysBeforeNow }.IsTodayPlayer.Should().Be(expected);
        }

        [Fact]
        public void PlayerSubmissionDate_GoesBackFromTheProposalMomentAndDropsTheTime()
        {
            var request = new ProposalRequest
            {
                DaysBeforeNow = 3,
                ProposalDateTime = new DateTime(2026, 9, 2, 18, 30, 0)
            };

            request.PlayerSubmissionDate.Should().Be(new DateTime(2026, 8, 30));
        }

        [Fact]
        public void ToDto_CarriesTypeValueUserAndIp()
        {
            var request = new ProposalRequest
            {
                Value = "Zidane",
                ProposalType = ProposalTypes.Name,
                ProposalDateTime = new DateTime(2026, 9, 2, 18, 0, 0),
                Ip = "::1"
            };

            var dto = request.ToDto(7, true);

            dto.UserId.Should().Be(7);
            dto.Value.Should().Be("Zidane");
            dto.ProposalTypeId.Should().Be((ulong)ProposalTypes.Name);
            dto.Successful.Should().Be(1);
            dto.Ip.Should().Be("::1");
            dto.ProposalDate.Should().Be(new DateTime(2026, 9, 2));
        }

        [Fact]
        public void ToDto_WhenUnsuccessful_FlagsZero()
        {
            var request = new ProposalRequest { Value = "x", ProposalType = ProposalTypes.Name };

            request.ToDto(7, false).Successful.Should().Be(0);
        }

        [Fact]
        public void MatchAny_MatchesOnTypeAndValue()
        {
            var request = new ProposalRequest { Value = "Zidane", ProposalType = ProposalTypes.Name };

            var existing = new List<ProposalDto>
            {
                ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)ProposalTypes.Club).WithValue("Zidane").Build(),
                ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)ProposalTypes.Name).WithValue("Zidane").Build()
            };

            request.MatchAny(existing).Should().BeTrue();
        }

        [Theory]
        // la detection de doublon doit etre aussi tolerante que la detection de reussite,
        // sinon une variante de casse ou d'accent est facturee une seconde fois
        [InlineData("Zidane")]
        [InlineData("zidane")]
        [InlineData("ZIDANE")]
        [InlineData("Zidàne")]
        [InlineData("  Zidane  ")]
        public void MatchAny_IgnoresCaseAccentsAndSurroundingSpaces(string value)
        {
            var request = new ProposalRequest { Value = value, ProposalType = ProposalTypes.Name };

            var existing = new List<ProposalDto>
            {
                ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)ProposalTypes.Name).WithValue("Zidane").Build()
            };

            request.MatchAny(existing).Should().BeTrue();
        }

        [Fact]
        public void MatchAny_AppliesToFreeTextClubsToo()
        {
            // le champ club est un texte libre : l'autocompletion suggere mais ne
            // contraint pas, donc les variantes de casse y sont possibles
            var request = new ProposalRequest { Value = "real madrid", ProposalType = ProposalTypes.Club };

            var existing = new List<ProposalDto>
            {
                ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)ProposalTypes.Club).WithValue("Real Madrid").Build()
            };

            request.MatchAny(existing).Should().BeTrue();
        }

        [Fact]
        public void MatchAny_StillDistinguishesGenuinelyDifferentValues()
        {
            var request = new ProposalRequest { Value = "Ronaldo", ProposalType = ProposalTypes.Name };

            var existing = new List<ProposalDto>
            {
                ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)ProposalTypes.Name).WithValue("Zidane").Build()
            };

            request.MatchAny(existing).Should().BeFalse();
        }

        [Fact]
        public void MatchAny_HandlesNullValuesWithoutThrowing()
        {
            // proposals.value est nullable en base
            var request = new ProposalRequest { Value = null, ProposalType = ProposalTypes.Clue };

            var existing = new List<ProposalDto>
            {
                ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)ProposalTypes.Clue).WithValue(null).Build()
            };

            request.MatchAny(existing).Should().BeTrue();
            request.MatchAny(new List<ProposalDto>
            {
                ProposalDtoBuilder.Valid().WithProposalTypeId((ulong)ProposalTypes.Clue).WithValue("x").Build()
            }).Should().BeFalse();
        }

        [Fact]
        public void MatchAny_WhenNothingMatches_ReturnsFalse()
        {
            var request = new ProposalRequest { Value = "Zidane", ProposalType = ProposalTypes.Name };

            request.MatchAny(new List<ProposalDto>()).Should().BeFalse();
        }
    }

    internal static class Localizer
    {
        /// <summary>Localizer qui renvoie la cle telle quelle, pour asserter dessus.</summary>
        internal static IStringLocalizer Echo()
        {
            var mock = new Mock<IStringLocalizer>();
            mock.Setup(_ => _[It.IsAny<string>()])
                .Returns<string>(key => new LocalizedString(key, key));
            return mock.Object;
        }
    }
}
