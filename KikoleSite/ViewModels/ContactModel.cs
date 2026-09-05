using System.Collections.Generic;
using KikoleSite.Models.Dtos;

namespace KikoleSite.ViewModels;

public class ContactModel
{
    public IReadOnlyCollection<DiscussionMessageDto> Messages { get; set; } = [];

    public string? NewMessage { get; set; }

    public string? ErrorMessage { get; set; }
}
