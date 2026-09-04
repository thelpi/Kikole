using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using Microsoft.Extensions.Configuration;

namespace KikoleSite.Repositories;

public class ClubRepository : BaseRepository, IClubRepository
{
    public ClubRepository(IConfiguration configuration, IClock clock)
        : base(configuration, clock)
    { }

    public async Task<ulong> CreateClubAsync(ClubDto club)
    {
        return await ExecuteInsertAsync(
                "clubs",
                ("name", club.Name),
                ("allowed_names", club.AllowedNames),
                ("country_id", club.CountryId),
                ("creation_date", Clock.Now));
    }

    public async Task UpdateClubAsync(ClubDto club)
    {
        await ExecuteNonQueryAsync(
                "UPDATE clubs " +
                "SET name = @name, allowed_names = @allowed_names, country_id = @country_id " +
                "WHERE id = @id",
                new
                {
                    name = club.Name,
                    allowed_names = club.AllowedNames,
                    country_id = club.CountryId,
                    id = club.Id
                });
    }

    public async Task<ClubDto?> GetClubAsync(ulong clubId)
    {
        return await GetDtoAsync<ClubDto>(
                "clubs",
                ("id", clubId));
    }

    public async Task<IReadOnlyCollection<ClubDto>> GetClubsByIdsAsync(IReadOnlyCollection<ulong> clubIds)
    {
        if (clubIds.Count == 0)
            return [];

        return await ExecuteReaderAsync<ClubDto>(
                "SELECT * FROM clubs WHERE id IN @clubIds",
                new { clubIds });
    }

    public async Task<IReadOnlyCollection<ClubDto>> GetClubsAsync()
    {
        return await GetDtosAsync<ClubDto>(
                "clubs");
    }
}
