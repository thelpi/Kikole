using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

        internal static bool IsPropertyExist(dynamic settings, string name)
        {
            return settings is ExpandoObject
                ? ((IDictionary<string, object>)settings).ContainsKey(name)
                : settings.GetType().GetProperty(name) != null;
        }

        public static string ToNaString(this object data)
        {
            return data?.ToString() ?? NA;
        }

        internal static string ToNaString(this TimeSpan? data)
        {
            return data?.ToString(TimeSpanPattern) ?? NA;
        }

        public static string ToNaString(this TimeSpan data)
        {
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
            switch (date.Month)
            {
                case 1: return IsFrench() ? "Janvier" : "January";
                case 2: return IsFrench() ? "Février" : "February";
                case 3: return IsFrench() ? "Mars" : "March";
                case 4: return IsFrench() ? "Avril" : "April";
                case 5: return IsFrench() ? "Mai" : "May";
                case 6: return IsFrench() ? "Juin" : "June";
                case 7: return IsFrench() ? "Juillet" : "July";
                case 8: return IsFrench() ? "Août" : "August";
                case 9: return IsFrench() ? "Septembre" : "September";
                case 10: return IsFrench() ? "Octobre" : "October";
                case 11: return IsFrench() ? "Novembre" : "November";
                case 12: return IsFrench() ? "Décembre" : "December";
                default: throw new NotImplementedException();
            }
        }

        public static string GetLabel(this Api.LeaderSort sort)
        {
            switch (sort)
            {
                case Api.LeaderSort.BestTime:
                    return IsFrench() ? "Meilleur temps" : "Best time";
                case Api.LeaderSort.SuccessCount:
                    return IsFrench() ? "Nombre de succès" : "Success count";
                case Api.LeaderSort.TotalPoints:
                    return IsFrench() ? "Points" : "Points";
                default:
                    throw new NotImplementedException();
            }
        }

        private static bool IsFrench()
        {
            return GetLanguage() == Api.Languages.fr;
        }

        internal static Api.Languages GetLanguage()
        {
            if (!Enum.TryParse<Api.Languages>(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, out var language))
                return Api.Languages.en;
            return language;
        }

        internal static string GetLabel(this Api.ProposalType type, bool withFrenchDe)
        {
            switch (type)
            {
                case Api.ProposalType.Name:
                    return IsFrench() ? $"{(withFrenchDe ? "de " : "")}nom" : "name";
                case Api.ProposalType.Club:
                    return IsFrench() ? $"{(withFrenchDe ? "de " : "")}club" : "club";
                case Api.ProposalType.Year:
                    return IsFrench() ? $"{(withFrenchDe ? "d'" : "")}année" : "year";
                case Api.ProposalType.Country:
                    return IsFrench() ? $"{(withFrenchDe ? "de " : "")}nationalité" : "country";
                case Api.ProposalType.Position:
                    return IsFrench() ? $"{(withFrenchDe ? "de " : "")}position" : "position";
                default:
                    throw new NotImplementedException();
            }
        }

        internal static string GetLabel(this Api.Position position)
        {
            switch (position)
            {
                case Api.Position.Defender:
                    return IsFrench() ? "Défenseur" : "Defender";
                case Api.Position.Forward:
                    return IsFrench() ? "Attaquant" : "Forward";
                case Api.Position.Goalkeeper:
                    return IsFrench() ? "Gardien de but" : "Goalkeeper";
                case Api.Position.Midfielder:
                    return IsFrench() ? "Milieu de terrain" : "Midfielder";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
