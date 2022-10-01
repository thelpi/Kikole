using System.Collections.Generic;

namespace KikoleSite.Api.Models
{
    public class Palmares
    {
        public IReadOnlyDictionary<(int month, int year), (User first, User second, User third)> MonthlyPalmares { get; set; }

        public IReadOnlyCollection<(User user, int first, int second, int third)> GlobalPalmares { get; set; }
    }
}
