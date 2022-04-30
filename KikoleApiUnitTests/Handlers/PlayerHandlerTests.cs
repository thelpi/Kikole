using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleApi.Handlers;
using KikoleApi.Interfaces.Repositories;
using KikoleApi.Models.Dtos;
using Moq;
using Xunit;

namespace KikoleApiUnitTests.Handlers
{
    public class PlayerHandlerTests
    {
        private readonly PlayerHandler _playerHandler;
        private readonly Mock<IPlayerRepository> _playerRepositoryMock;
        private readonly Mock<IClubRepository> _clubRepositoryMock;

        public PlayerHandlerTests()
        {
            _playerRepositoryMock = new Mock<IPlayerRepository>();
            _clubRepositoryMock = new Mock<IClubRepository>();
            _playerHandler = new PlayerHandler(_playerRepositoryMock.Object, _clubRepositoryMock.Object);
        }

        [Fact]
        public async Task GetPlayerFullInfoAsync_Nominal_ReturnsFullPlayer()
        {
            var pDto = new PlayerDto
            {
                Id = 1
            };

            var pcDtos = new List<PlayerClubDto>
            {
                new PlayerClubDto { ClubId = 2 },
                new PlayerClubDto { ClubId = 3 },
                new PlayerClubDto { ClubId = 4 },
            };

            var cDtos = new List<ClubDto>
            {
                new ClubDto { Name = "Riri" },
                new ClubDto { Name = "Fifi" },
                new ClubDto { Name = "Loulou" }
            };

            _playerRepositoryMock
                .Setup(_ => _.GetPlayerClubsAsync(1))
                .ReturnsAsync(pcDtos);

            _clubRepositoryMock
                .Setup(_ => _.GetClubAsync(2))
                .ReturnsAsync(cDtos[0]);
            _clubRepositoryMock
                .Setup(_ => _.GetClubAsync(3))
                .ReturnsAsync(cDtos[1]);
            _clubRepositoryMock
                .Setup(_ => _.GetClubAsync(4))
                .ReturnsAsync(cDtos[2]);

            var expected = new PlayerFullDto
            {
                Clubs = cDtos,
                Player = pDto,
                PlayerClubs = pcDtos
            };

            var result = await _playerHandler
                .GetPlayerFullInfoAsync(pDto)
                .ConfigureAwait(false);

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetPlayerOfTheDayFullInfoAsync_Nominal_ReturnsFullPlayer()
        {
            var dt = DateTime.Today;

            var pDto = new PlayerDto
            {
                Id = 1
            };

            var pcDtos = new List<PlayerClubDto>
            {
                new PlayerClubDto { ClubId = 2 },
                new PlayerClubDto { ClubId = 3 },
                new PlayerClubDto { ClubId = 4 },
            };

            var cDtos = new List<ClubDto>
            {
                new ClubDto { Name = "Riri" },
                new ClubDto { Name = "Fifi" },
                new ClubDto { Name = "Loulou" }
            };

            _playerRepositoryMock
                .Setup(_ => _.GetPlayerOfTheDayAsync(dt))
                .ReturnsAsync(pDto);

            _playerRepositoryMock
                .Setup(_ => _.GetPlayerClubsAsync(1))
                .ReturnsAsync(pcDtos);

            _clubRepositoryMock
                .Setup(_ => _.GetClubAsync(2))
                .ReturnsAsync(cDtos[0]);
            _clubRepositoryMock
                .Setup(_ => _.GetClubAsync(3))
                .ReturnsAsync(cDtos[1]);
            _clubRepositoryMock
                .Setup(_ => _.GetClubAsync(4))
                .ReturnsAsync(cDtos[2]);

            var expected = new PlayerFullDto
            {
                Clubs = cDtos,
                Player = pDto,
                PlayerClubs = pcDtos
            };

            var result = await _playerHandler
                .GetPlayerOfTheDayFullInfoAsync(dt)
                .ConfigureAwait(false);

            result.Should().BeEquivalentTo(expected);
        }
    }
}
