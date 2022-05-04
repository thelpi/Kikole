using System;

namespace KikoleSite.Api.Models.Dtos
{
    public class KikoleAwardDto
    {
        public string Name { get; set; }

        public DateTime ProposalDate { get; set; }

        public decimal AvgPts { get; set; }
    }
}
