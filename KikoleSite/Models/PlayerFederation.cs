using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Dtos;

namespace KikoleSite.Models
{
    public class PlayerFederation
    {
        public string Name { get; }

        internal PlayerFederation(PlayerFederationDto playerClub, IEnumerable<Federation> federations)
        {
            Name = federations.Single(c => (ulong)c.Code == playerClub.FederationId).Name;
        }
    }
}
