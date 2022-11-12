using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Models
{
    public class PlayerFederation
    {
        public string Name { get; }

        internal PlayerFederation(ulong federationId, IEnumerable<Federation> federations)
        {
            Name = federations.Single(c => (ulong)c.Code == federationId).Name;
        }
    }
}
