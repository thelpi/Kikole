using System.Collections.Generic;

namespace KikoleSite.Models
{
    public class MainModel
    {
        public bool HasWrongGuess { get; set; }
        public string YearOkSubmitted { get; set; }
        public string CountryOkSubmitted { get; set; }
        public string NameOkSubmitted { get; set; }
        public string ClubsOkSubmitted { get; set; }
        public string ClubsOkCurrentlySelected { get; set; }
        public Country SelectedValueCountry { get; set; }
        public string SelectedValueName { get; set; }
        public string SelectedValueClub { get; set; }
        public string SelectedValueYear { get; set; }
        public Dictionary<Country, string> Countries { get; set; }
    }
}
