using System.Collections.Generic;
using KikoleSite.Models.Dtos;

namespace KikoleSite.ViewModels;

public class AdminDiscussionsModel
{
    public IReadOnlyCollection<DiscussionSummaryDto> Discussions { get; set; } = [];
}
