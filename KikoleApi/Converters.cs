using System.Collections.Generic;
using System.Linq;
using KikoleApi.Domain.Models;
using KikoleApi.Domain.Models.Dtos;

namespace KikoleApi
{
    internal static class Converters
    {
        internal static IReadOnlyCollection<PlayerClubDto> ToDtos(this IReadOnlyCollection<ClubRequest> clubs, long playerId)
        {
            return clubs
                .Select(c => c.ToDto(playerId))
                .ToList();
        }

        private static PlayerClubDto ToDto(this ClubRequest club, long playerId)
        {
            return new PlayerClubDto
            {
                AllowedNames = club.AllowedNames.SanitizeJoin(),
                HistoryPosition = club.HistoryPosition,
                ImportancePosition = club.ImportancePosition,
                Name = club.Name,
                PlayerId = playerId
            };
        }

        internal static IReadOnlyCollection<PlayerNameDto> ToDtos(this IEnumerable<string> playerNames, long playerId)
        {
            return playerNames
                .Select(pn => new PlayerNameDto
                {
                    Name = pn.Sanitize(),
                    PlayerId = playerId
                })
                .ToList();
        }

        internal static PlayerDto ToDto(this PlayerRequest request)
        {
            return new PlayerDto
            {
                Country1Id = (ulong)request.Country,
                Country2Id = (ulong?)request.SecondCountry,
                Name = request.Name,
                ProposalDate = request.ProposalDate,
                YearOfBirth = (uint)request.DateOfBirth.Year
            };
        }
    }
}
