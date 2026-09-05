using System.Collections.Generic;
using KikoleSite.Models.Dtos;

namespace KikoleSite.ViewModels;

public class AdminDiscussionModel
{
    public ulong DiscussionId { get; set; }

    public required string UserLogin { get; set; }

    public IReadOnlyCollection<DiscussionMessageDto> Messages { get; set; } = [];

    public string? NewMessage { get; set; }

    public string? ErrorMessage { get; set; }
}
