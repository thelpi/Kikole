using FluentAssertions;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Xunit;

namespace KikoleSiteUnitTests.Models
{
    /// <summary>
    /// Regles d'anonymisation du joueur du jour. Un faux positif ici revient a spoiler
    /// la reponse, c'est la partie la plus sensible du modele.
    /// </summary>
    public class PlayerCreatorTests
    {
        private const ulong CreatorId = 100;
        private const ulong OtherUserId = 200;

        private static PlayerDto Player(byte hideCreator = 0)
        {
            return new PlayerDto
            {
                Id = 1,
                Name = "Zinédine Zidane",
                AllowedNames = "zidane;zizou",
                CreationUserId = CreatorId,
                HideCreator = hideCreator
            };
        }

        private static UserDto User(ulong id, UserTypes type)
        {
            return new UserDto { Id = id, Login = "login" + id, UserTypeId = (ulong)type };
        }

        // ------------------------------------------------------------- revelation du nom

        [Fact]
        public void WhenTheRequesterIsTheCreator_TheAnswerIsRevealed()
        {
            var result = new PlayerCreator(
                User(CreatorId, UserTypes.StandardUser), Player(), User(CreatorId, UserTypes.StandardUser));

            result.Name.Should().Be("Zinédine Zidane");
            result.AllowedNames.Should().BeEquivalentTo(new[] { "zidane", "zizou" });
        }

        [Fact]
        public void WhenTheRequesterIsAdministrator_TheAnswerIsRevealed()
        {
            var result = new PlayerCreator(
                User(OtherUserId, UserTypes.Administrator), Player(), User(CreatorId, UserTypes.StandardUser));

            result.Name.Should().Be("Zinédine Zidane");
            result.AllowedNames.Should().NotBeNull();
        }

        [Theory]
        [InlineData(UserTypes.StandardUser)]
        [InlineData(UserTypes.PowerUser)]
        public void WhenTheRequesterIsAnyoneElse_TheAnswerIsHidden(UserTypes type)
        {
            // un power user n'a pas plus de droits qu'un joueur standard sur la reponse
            var result = new PlayerCreator(
                User(OtherUserId, type), Player(), User(CreatorId, UserTypes.StandardUser));

            result.Name.Should().BeNull();
            result.AllowedNames.Should().BeNull();
        }

        // ------------------------------------------------------------- attribution du createur

        [Fact]
        public void TheCreatorLoginIsOnlyExposedForAPowerUser()
        {
            var asPowerUser = new PlayerCreator(
                User(OtherUserId, UserTypes.StandardUser), Player(), User(CreatorId, UserTypes.PowerUser));

            asPowerUser.Login.Should().Be("login" + CreatorId);
        }

        [Theory]
        [InlineData(UserTypes.StandardUser)]
        [InlineData(UserTypes.Administrator)]
        public void ACreatorWhoIsNotAPowerUserIsNotNamed(UserTypes creatorType)
        {
            var result = new PlayerCreator(
                User(OtherUserId, UserTypes.StandardUser), Player(), User(CreatorId, creatorType));

            result.Login.Should().BeNull();
        }

        // ------------------------------------------------------------- anonymat volontaire

        [Theory]
        [InlineData((byte)0, true)]
        [InlineData((byte)1, false)]
        public void CanDisplayCreator_FollowsTheHideCreatorFlag(byte hideCreator, bool expected)
        {
            var result = new PlayerCreator(
                User(OtherUserId, UserTypes.StandardUser), Player(hideCreator), User(CreatorId, UserTypes.PowerUser));

            result.CanDisplayCreator.Should().Be(expected);
        }

        [Fact]
        public void HidingTheCreatorDoesNotHideTheAnswerFromTheCreatorHimself()
        {
            // les deux regles sont independantes
            var result = new PlayerCreator(
                User(CreatorId, UserTypes.StandardUser), Player(hideCreator: 1), User(CreatorId, UserTypes.StandardUser));

            result.CanDisplayCreator.Should().BeFalse();
            result.Name.Should().Be("Zinédine Zidane");
        }

        [Fact]
        public void ThePlayerIdIsAlwaysExposed()
        {
            var result = new PlayerCreator(
                User(OtherUserId, UserTypes.StandardUser), Player(), User(CreatorId, UserTypes.StandardUser));

            result.PlayerId.Should().Be(1);
        }
    }
}
