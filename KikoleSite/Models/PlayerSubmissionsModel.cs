using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Models
{
    public class PlayerSubmissionsModel
    {
        public IReadOnlyCollection<PlayerSubmissionModel> Players { get; set; }

        public ulong SelectedId { get; set; }

        public PlayerSubmissionModel SelectedPlayer => Players?.SingleOrDefault(p => p.Id == SelectedId);

        public string ErrorMessage { get; set; }

        public string InfoMessage { get; set; }

        public string ClueOverwriteEn { get; set; }

        public string ClueOverwriteFr { get; set; }

        public string RefusalReason { get; set; }
    }
}
