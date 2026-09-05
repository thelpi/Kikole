using System.Collections.Generic;

namespace KikoleSite.Models;

public class Podiums
{
    public required IReadOnlyDictionary<(int month, int year), (User first, User second, User third)> MonthlyPodiums { get; set; }

    public required IReadOnlyCollection<(User user, int first, int second, int third)> OverallPodium { get; set; }
}
