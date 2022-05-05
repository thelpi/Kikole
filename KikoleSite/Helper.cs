using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using KikoleSite.Api.Models.Enums;
using Microsoft.Extensions.Configuration;

namespace KikoleSite
{
    public static class Helper
    {
        public const string NA = "N/A";
        public const string TimeSpanPattern = @"hh\:mm";
        public const string DateTimePatternEn = "yyyy-MM-dd";
        public const string DateTimePatternFr = "dd/MM/yyyy";
        public const string Iso8859Code = "ISO-8859-8";

        static readonly string EncryptionKey = Startup.StaticConfig.GetValue<string>("EncryptionCookieKey");

        public static string ToNaString(this object data)
        {
            return data?.ToString() ?? NA;
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

        internal static string Encrypt(this string plainText)
        {
            try
            {
                byte[] array;
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                    aes.IV = new byte[16];
                    var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (var streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }
                            array = memoryStream.ToArray();
                        }
                    }
                }
                return Convert.ToBase64String(array);
            }
            catch
            {
                // TODO: log
                return plainText;
            }
        }

        internal static string Decrypt(this string encryptedText)
        {
            try
            {
                var buffer = Convert.FromBase64String(encryptedText);
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                    aes.IV = new byte[16];
                    var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (var memoryStream = new MemoryStream(buffer))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (var streamReader = new StreamReader(cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                // TODO: log
                return encryptedText;
            }
        }

        internal static string Sanitize(this string value)
        {
            return value.Trim().RemoveDiacritics().ToLowerInvariant();
        }

        internal static string RemoveDiacritics(this string value)
        {
            var tempBytes = Encoding.GetEncoding(Iso8859Code).GetBytes(value);
            return Encoding.UTF8.GetString(tempBytes);
        }

        internal static DateTime Min(this DateTime dt1, DateTime dt2)
        {
            return dt1 > dt2 ? dt2 : dt1;
        }

        internal static DateTime Max(this DateTime dt1, DateTime dt2)
        {
            return dt1 < dt2 ? dt2 : dt1;
        }

        internal static bool IsToday(this DateTime date)
        {
            return date.Date == DateTime.Now.Date;
        }

        internal static bool IsFirstOfMonth(this DateTime date, DateTime? reference = null)
        {
            reference = (reference ?? DateTime.Now).Date;
            date = date.Date;
            return date.Year == reference.Value.Year
                && date.Month == reference.Value.Month
                && date.Day == 1;
        }

        internal static bool IsAfterInMonth(this DateTime date, DateTime? reference = null)
        {
            reference = (reference ?? DateTime.Now).Date;
            date = date.Date;
            return date.Year == reference.Value.Year
                && date.Month == reference.Value.Month
                && date.Day >= reference.Value.Day;
        }

        internal static bool IsEndOfMonth(this DateTime date, DateTime? reference = null)
        {
            reference = (reference ?? DateTime.Now).Date;
            date = date.Date;
            return date.Year == reference.Value.Year
                && date.Month == reference.Value.Month
                && date.AddDays(1).Month > reference.Value.Month;
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
                DayLeaderSorts.TotalPointsOverall => IsFrench() ? "Points (inc. hors-délai)" : "Points (inc. out of time)",
                _ => throw new NotImplementedException(),
            };
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
    }
}
