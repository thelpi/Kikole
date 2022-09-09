using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Repositories
{
    public class CacheManager : ICacheManager
    {
        private readonly SemaphoreSlim _disableCacheSempahore;
        private readonly SemaphoreSlim _cacheAccessSempahore;

        private readonly IReadRepository _repository;
        private readonly Dictionary<(Stage, Level), IReadOnlyCollection<EntryDto>> _entries;

        private bool _disableCache;

        public CacheManager(IReadRepository repository)
        {
            _repository = repository;

            _entries = new Dictionary<(Stage, Level), IReadOnlyCollection<EntryDto>>();
            _disableCache = false;

            _cacheAccessSempahore = new SemaphoreSlim(1);
            _disableCacheSempahore = new SemaphoreSlim(1);
        }

        public async Task<IReadOnlyCollection<EntryDto>> GetStageLevelEntriesAsync(Stage stage, Level level)
        {
            if (_disableCache)
            {
                return await _repository
                    .GetEntriesAsync(stage, level, null, null)
                    .ConfigureAwait(false);
            }

            if (!_entries.ContainsKey((stage, level)))
                await SetCacheEntriesInternalAsync(stage, level).ConfigureAwait(false);

            return _entries[(stage, level)];
        }

        private async Task SetCacheEntriesInternalAsync(Stage stage, Level level)
        {
            await _cacheAccessSempahore
                .WaitAsync()
                .ConfigureAwait(false);

            if (!_entries.ContainsKey((stage, level)))
            {
                var entries = await _repository
                    .GetEntriesAsync(stage, level, null, null)
                    .ConfigureAwait(false);

                _entries.Add((stage, level), entries);
            }

            _cacheAccessSempahore.Release();
        }

        public async Task ToggleCacheLockAsync(bool newValue)
        {
            await _disableCacheSempahore.WaitAsync().ConfigureAwait(false);

            _disableCache = newValue;
            _entries.Clear();

            if (!_disableCache)
            {
                foreach (var stage in SystemExtensions.Enumerate<Stage>())
                {
                    foreach (var level in SystemExtensions.Enumerate<Level>())
                        await SetCacheEntriesInternalAsync(stage, level).ConfigureAwait(false);
                }
            }

            _disableCacheSempahore.Release();
        }
    }
}
