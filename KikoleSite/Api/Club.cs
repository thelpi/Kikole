using System.Collections.Generic;

namespace KikoleSite.Api
{
    public class Club
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public IReadOnlyList<string> AllowedNames { get; set; }
    }
}
