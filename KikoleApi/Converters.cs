using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;

namespace KikoleApi
{
    internal static class Converters
    {
        internal static IReadOnlyCollection<PlayerClubDto> ToDtos(this IReadOnlyCollection<PlayerClubRequest> clubs, ulong playerId)
        {
            return clubs
                .Select(c => c.ToDto(playerId))
                .ToList();
        }

        private static PlayerClubDto ToDto(this PlayerClubRequest club, ulong playerId)
        {
            return new PlayerClubDto
            {
                HistoryPosition = club.HistoryPosition,
                ImportancePosition = club.ImportancePosition,
                ClubId = club.ClubId,
                PlayerId = playerId
            };
        }

        internal static PlayerDto ToDto(this PlayerRequest request)
        {
            return new PlayerDto
            {
                Country1Id = (ulong)request.Country,
                Country2Id = (ulong?)request.SecondCountry,
                Name = request.Name,
                ProposalDate = request.ProposalDate,
                YearOfBirth = (uint)request.DateOfBirth.Year,
                AllowedNames = request.AllowedNames.SanitizeJoin(request.Name)
            };
        }

        internal static ClubDto ToDto(this ClubRequest request)
        {
            return new ClubDto
            {
                AllowedNames = request.AllowedNames.SanitizeJoin(request.Name),
                Name = request.Name
            };
        }
    }
}
