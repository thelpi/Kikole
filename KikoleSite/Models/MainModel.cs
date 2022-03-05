using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Models
{
    public class MainModel
    {
        public string ClubsOkSerializedInput => ClubsOkSubmitted == null
            ? null
            : string.Join(';', ClubsOkSubmitted.Select(cc => $"{cc.Name}§{cc.HistoryPosition}§{cc.ImportancePosition}"));

        public string ClubsOkSerializedOutput { get; set; }
        public bool HasWrongGuess { get; set; }
        public string YearOkSubmitted { get; set; }
        public string CountryOkSubmitted { get; set; }
        public string NameOkSubmitted { get; set; }
        public List<PlayerClub> ClubsOkSubmitted { get; set; }
        public string ClubsOkCurrentlySelected { get; set; }
        public Country SelectedValueCountry { get; set; }
        public string SelectedValueName { get; set; }
        public string SelectedValueClub { get; set; }
        public string SelectedValueYear { get; set; }
        public Dictionary<Country, string> Countries { get; set; }

        internal List<PlayerClub> ToClubsOkSubmitted()
        {
            return ClubsOkSerializedOutput == null
                ? null
                : ClubsOkSerializedOutput
                    .Split(';')
                    .Select(_ => new PlayerClub
                    {
                        HistoryPosition = byte.Parse(_.Split('§')[1]),
                        ImportancePosition = byte.Parse(_.Split('§')[2]),
                        Name = _.Split('§')[0]
                    })
                    .ToList();
        }
    }
}
