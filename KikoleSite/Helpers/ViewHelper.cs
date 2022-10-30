using System;
using System.Globalization;
using KikoleSite.Models.Enums;

namespace KikoleSite.Helpers
{
    public static class ViewHelper
    {
        public const string NA = "N/A";
        public const string TimeSpanPattern = @"hh\:mm";
        public const string DateTimePatternEn = "yyyy-MM-dd";
        public const string DateTimePatternFr = "dd/MM/yyyy";
        public const string DayPatternEn = "MM-dd";
        public const string DayPatternFr = "dd/MM";
        public const string Iso8859Code = "ISO-8859-8";

        public static string ToNaString(this object data)
        {
            if (data == null)
                return NA;

            if (data.GetType() == typeof(TimeSpan) || data.GetType() == typeof(TimeSpan?))
                return ((TimeSpan?)data).ToNaString();

            if (data.GetType() == typeof(DateTime) || data.GetType() == typeof(DateTime?))
                return ((DateTime?)data).ToNaString();

            if (data.GetType() == typeof(bool))
                return ((bool)data).ToYesNo();

            return data.ToString();
        }

        internal static string ToNaString(this TimeSpan? data)
        {
            return data?.ToNaString() ?? NA;
        }

        public static string ToNaString(this TimeSpan data)
        {
            if (data.TotalHours >= 24)
            {
                var days = (int)Math.Floor(data.TotalDays);
                var label = IsFrench() ? "jour" : "day";
                if (days > 1)
                    label += "s";
                return $"{days} {label}";
            }

            return data.ToString(TimeSpanPattern);
        }

        internal static string ToNaString(this DateTime? data)
        {
            return data?.ToString(IsFrench() ? DateTimePatternFr : DateTimePatternEn) ?? NA;
        }

        public static string ToNaString(this DateTime data)
        {
            return data.ToString(IsFrench() ? DateTimePatternFr : DateTimePatternEn);
        }

        public static string ToYesNo(this bool data)
        {
            return data
                ? (IsFrench() ? "Oui" : "Yes")
                : (IsFrench() ? "Non" : "No");
        }

        public static string ToYesNo(this bool? data)
        {
            return !data.HasValue
                ? NA
                : data.Value.ToYesNo();
        }

        internal static string GetMonthName(this DateTime date)
        {
            return date.Month switch
            {
                1 => IsFrench() ? "Janvier" : "January",
                2 => IsFrench() ? "Février" : "February",
                3 => IsFrench() ? "Mars" : "March",
                4 => IsFrench() ? "Avril" : "April",
                5 => IsFrench() ? "Mai" : "May",
                6 => IsFrench() ? "Juin" : "June",
                7 => IsFrench() ? "Juillet" : "July",
                8 => IsFrench() ? "Août" : "August",
                9 => IsFrench() ? "Septembre" : "September",
                10 => IsFrench() ? "Octobre" : "October",
                11 => IsFrench() ? "Novembre" : "November",
                12 => IsFrench() ? "Décembre" : "December",
                _ => throw new NotImplementedException(),
            };
        }

        public static string GetLabel(this LeaderSorts sort)
        {
            return sort switch
            {
                LeaderSorts.BestTime => IsFrench() ? "Meilleur temps" : "Best time",
                LeaderSorts.SuccessCount => IsFrench() ? "Nombre de succès" : "Success count",
                LeaderSorts.TotalPoints => IsFrench() ? "Points" : "Points",
                LeaderSorts.SuccessCountOverall => IsFrench() ? "Nombre de succès (inc. hors-délai)" : "Success count (inc. out of time)",
                LeaderSorts.TotalPointsOverall => IsFrench() ? "Points (inc. hors-délai)" : "Points (inc. out of time)",
                _ => throw new NotImplementedException(),
            };
        }

        public static string GetLabel(this DayLeaderSorts sort)
        {
            return sort switch
            {
                DayLeaderSorts.BestTime => IsFrench() ? "Meilleur temps" : "Best time",
                DayLeaderSorts.TotalPoints => IsFrench() ? "Points" : "Points",
                _ => throw new NotImplementedException(),
            };
        }

        internal static string GetNumDayLabel(this DateTime date)
        {
            return IsFrench()
                ? date.ToString(DayPatternFr)
                : date.ToString(DayPatternEn);
        }

        private static bool IsFrench()
        {
            return CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "fr";
        }

        internal static string GetLabel(this ProposalTypes type, bool withFrenchDe)
        {
            return type switch
            {
                ProposalTypes.Name => IsFrench() ? $"{(withFrenchDe ? "de " : "")}nom" : "name",
                ProposalTypes.Club => IsFrench() ? $"{(withFrenchDe ? "de " : "")}club" : "club",
                ProposalTypes.Year => IsFrench() ? $"{(withFrenchDe ? "d'" : "")}année" : "year",
                ProposalTypes.Country => IsFrench() ? $"{(withFrenchDe ? "de " : "")}nationalité" : "country",
                ProposalTypes.Continent => IsFrench() ? $"{(withFrenchDe ? "de " : "")}continent" : "continent",
                ProposalTypes.Position => IsFrench() ? $"{(withFrenchDe ? "de " : "")}position" : "position",
                _ => throw new NotImplementedException(),
            };
        }

        internal static string GetLabel(this Positions position)
        {
            return position switch
            {
                Positions.Defender => IsFrench() ? "Défenseur" : "Defender",
                Positions.Forward => IsFrench() ? "Attaquant" : "Forward",
                Positions.Goalkeeper => IsFrench() ? "Gardien de but" : "Goalkeeper",
                Positions.Midfielder => IsFrench() ? "Milieu de terrain" : "Midfielder",
                _ => throw new NotImplementedException(),
            };
        }

        internal static Languages GetLanguage()
        {
            return CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "fr"
                ? Languages.fr
                : Languages.en;
        }
    }
}
