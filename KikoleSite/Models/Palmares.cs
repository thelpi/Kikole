using System.Collections.Generic;

namespace KikoleSite.Models
{
    public class Palmares
    {
        public required IReadOnlyDictionary<(int month, int year), (User first, User second, User third)> MonthlyPalmares { get; set; }

        public required IReadOnlyCollection<(User user, int first, int second, int third)> GlobalPalmares { get; set; }
    }
}
