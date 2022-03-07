using System.Collections.Generic;

namespace KikoleAdmin
{
    public class Club
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public IReadOnlyCollection<string> AllowedNames { get; set; }
    }
}
