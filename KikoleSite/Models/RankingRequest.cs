using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Dtos;
using KikoleSite.Enums;

namespace KikoleSite.Models
{
    public class RankingRequest
    {
        private DateTime _rankingDate;
        private DateTime? _rankingStartDate;
        private (long, DateTime)? _playerVsLegacy;
        private Dictionary<long, PlayerDto> _players;
        private readonly Dictionary<string, (long fakeId, List<long> playersId)> _countryPlayersGroup = new Dictionary<string, (long, List<long>)>();

        public Game Game { get; set; }

        public DateTime RankingDate
        {
            get => _rankingDate;
            set => _rankingDate = value.Date;
        }

        public DateTime? RankingStartDate
        {
            get => _rankingStartDate;
            set => _rankingStartDate = value?.Date;
        }

        public (long, DateTime)? PlayerVsLegacy
        {
            get => _playerVsLegacy;
            set => _playerVsLegacy = value.HasValue
                    ? (value.Value.Item1, value.Value.Item2.Date)
                    : default((long, DateTime)?);
        }

        public bool FullDetails { get; set; }

        public Engine? Engine { get; set; }

        public bool IncludeUnknownEngine { get; set; }

        public string Country { get; set; }

        public bool CountryGrouping { get; set; }

        internal IReadOnlyDictionary<long, PlayerDto> Players
        {
            get => _players;
            set
            {
                _players = value.ToDictionary(_ => _.Key, _ => _.Value);
                if (CountryGrouping)
                {
                    _players = MergePlayersIntoCountries();
                    CountryPlayersGroup = _countryPlayersGroup.Values.ToDictionary(_ => _.fakeId, _ => (IReadOnlyCollection<long>)_.playersId);
                }
            }
        }

        internal IReadOnlyDictionary<long, IReadOnlyCollection<long>> CountryPlayersGroup { get; private set; }

        private Dictionary<long, PlayerDto> MergePlayersIntoCountries()
        {
            var i = 1;
            foreach (var pId in _players.Keys)
            {
                var country = _players[pId].Country ?? string.Empty;
                if (!_countryPlayersGroup.ContainsKey(country))
                {
                    _countryPlayersGroup.Add(country, (i, new List<long>()));
                    i++;
                }
                _countryPlayersGroup[country].playersId.Add(pId);
            }

            var players = new List<PlayerDto>(_countryPlayersGroup.Count);

            var random = new Random();

            foreach (var country in _countryPlayersGroup.Keys)
            {
                var fillCountry = string.IsNullOrWhiteSpace(country) ? "N/A" : country;
                players.Add(new PlayerDto
                {
                    Color = $"{random.Next(0, 256):X2}{random.Next(0, 256):X2}{random.Next(0, 256):X2}",
                    Country = fillCountry,
                    Id = _countryPlayersGroup[country].fakeId,
                    RealName = fillCountry,
                    SurName = fillCountry,
                    UrlName = fillCountry
                });
            }

            return players.ToDictionary(r => r.Id, r => r);
        }
    }
}
