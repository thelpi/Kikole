using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KikoleSite.Models
{
    public class PlayerCreationModel
    {
        public string ErrorMessage { get; set; }

        public string InfoMessage { get; set; }

        public string Name { get; set; }

        public string AlternativeName0 { get; set; }

        public string AlternativeName1 { get; set; }

        public string AlternativeName2 { get; set; }

        public string AlternativeName3 { get; set; }

        public string AlternativeName4 { get; set; }

        public string AlternativeName5 { get; set; }

        public string AlternativeName6 { get; set; }

        public string AlternativeName7 { get; set; }

        public string AlternativeName8 { get; set; }

        public string AlternativeName9 { get; set; }

        public string YearOfBirth { get; set; }

        public string Country { get; set; }

        public string Club0 { get; set; }

        public string Club1 { get; set; }

        public string Club2 { get; set; }

        public string Club3 { get; set; }

        public string Club4 { get; set; }

        public string Club5 { get; set; }

        public string Club6 { get; set; }

        public string Club7 { get; set; }

        public string Club8 { get; set; }

        public string Club9 { get; set; }

        public string Position { get; set; }

        public IReadOnlyList<SelectListItem> Positions { get; set; }

        public string ClueFr { get; set; }

        public string ClueEn { get; set; }

        public string EasyClueFr { get; set; }

        public string EasyClueEn { get; set; }

        public bool DisplayPlayerSubmissionLink { get; set; }

        public bool HideCreator { get; set; }

        public Api.ProposalChart Chart { get; set; }
    }
}
