using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KikoleSite.Elite.ViewDatas
{
    public class IndexViewData
    {
        public IReadOnlyList<string> Countries { get; set; }

        public int Game { get; set; }

        public int ChronologyType { get; set; }

        public long? PlayerId { get; set; }

        public int? Engine { get; set; }

        public bool Anonymise { get; set; }

        // 1 TRUE, 0 NULL, -1 FALSE
        public int StillOngoing { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        public DateTime? RankingStartDate { get; set; }

        public int StandingType { get; set; }

        public long? SlayerPlayerId { get; set; }

        public string Country { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        public DateTime? RankingDate { get; set; }
    }
}
