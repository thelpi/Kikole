using System;

namespace KikoleSite.Api
{
    public class ProposalRequest
    {
        public DateTime ProposalDate { get; set; }

        public string Value { get; set; }

        public int DaysBefore { get; set; }
    }
}
