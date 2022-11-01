using System;
using System.Collections.Generic;

namespace KikoleSite.ViewModels
{
    public class UserDayModel
    {
        public string PlayerName { get; set; }
        public DateTime ProposalDate { get; set; }
        public string UserLogin { get; set; }
        public int UserScore { get; set; }
        public IReadOnlyCollection<UserDayItemModel> ProposalDetails { get; set; }
    }
}
