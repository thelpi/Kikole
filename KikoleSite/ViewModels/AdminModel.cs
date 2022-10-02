using System;
using System.Collections.Generic;

namespace KikoleSite.ViewModels
{
    public class AdminModel
    {
        public string Message { get; set; }
        public DateTime MessageDateStart { get; set; }
        public DateTime MessageDateEnd { get; set; }
        public string ActionFeedback { get; set; }
        public IReadOnlyCollection<Api.Models.Dtos.DiscussionDto> Discussions { get; set; }
    }
}
