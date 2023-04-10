using System.Collections.Generic;
using System.Threading.Tasks;
using KikoleSite.Dtos;
using KikoleSite.Enums;

namespace KikoleSite.Repositories
{
    public interface ITheEliteWebSiteParser
    {
        Task<IReadOnlyCollection<EntryWebDto>> GetMonthPageTimeEntriesAsync(int year, int month);

        Task<PlayerDto> GetPlayerInformationAsync(string urlName, string defaultHexPlayer);

        Task<IReadOnlyCollection<EntryWebDto>> GetPlayerEntriesAsync(Game game, string playerUrlName);

        Task<Engine> GetTimeEntryEngineAsync(string url);

        Task<IReadOnlyCollection<string>> GetPlayerUrlsAsync(Game game);
    }
}
