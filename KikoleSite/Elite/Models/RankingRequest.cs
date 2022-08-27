using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Models
{
    public class RankingRequest
    {
        private DateTime _rankingDate;
        private DateTime? _rankingStartDate;
        private (long, DateTime)? _playerVsLegacy;

        public Game Game { get; set; }

        public DateTime RankingDate
        {
            get { return _rankingDate; }
            set { _rankingDate = value.Date; }
        }

        public DateTime? RankingStartDate
        {
            get { return _rankingStartDate; }
            set { _rankingStartDate = value?.Date; }
        }

        public (long, DateTime)? PlayerVsLegacy
        {
            get { return _playerVsLegacy; }
            set
            {
                _playerVsLegacy = value.HasValue
                    ? (value.Value.Item1, value.Value.Item2.Date)
                    : default((long, DateTime)?);
            }
        }

        public bool FullDetails { get; set; }

        public Engine? Engine { get; set; }

        public bool IncludeUnknownEngine { get; set; }

        internal IReadOnlyDictionary<long, PlayerDto> Players { get; set; }

        internal ConcurrentDictionary<(Stage, Level), IReadOnlyCollection<EntryDto>> Entries { get; }
            = new ConcurrentDictionary<(Stage, Level), IReadOnlyCollection<EntryDto>>();
    }
}
