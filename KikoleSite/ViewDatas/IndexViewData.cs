using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KikoleSite.ViewDatas
{
    public class IndexViewData
    {
        public IReadOnlyList<SelectListItem> Countries { get; set; }

        public string ErrorMessage { get; set; }

        public int Game { get; set; }

        public int ChronologyType { get; set; }

        public uint? PlayerId { get; set; }

        public int? Engine { get; set; }

        public bool Anonymise { get; set; }

        // 1 TRUE, 0 NULL, -1 FALSE
        public int StillOngoing { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        public DateTime? RankingStartDate { get; set; }

        public int StandingType { get; set; }

        public uint? SlayerPlayerId { get; set; }

        public string Country { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Date only")]
        public DateTime? RankingDate { get; set; }

        public bool RemoveCurrentUntied { get; set; }

        public int MinimalPoints { get; set; }

        public bool DiscardEntryWhenBetter { get; set; }
    }
}
