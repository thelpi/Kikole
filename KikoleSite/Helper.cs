using System;
using System.Collections.Generic;
using System.Dynamic;
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
        public const string DateTimePattern = "yyyy-MM-dd";
        public const string Iso8859Code = "ISO-8859-8";

        static readonly string EncryptionKey = Startup.StaticConfig.GetValue<string>("EncryptionCookieKey");

        internal static bool IsPropertyExist(dynamic settings, string name)
        {
            return settings is ExpandoObject
                ? ((IDictionary<string, object>)settings).ContainsKey(name)
                : settings.GetType().GetProperty(name) != null;
        }

        internal static string ToNaString(this object data)
        {
            return data?.ToString() ?? NA;
        }

        internal static string ToNaString(this TimeSpan? data)
        {
            return data?.ToString(TimeSpanPattern) ?? NA;
        }

        internal static string ToNaString(this DateTime? data)
        {
            return data?.ToString(DateTimePattern) ?? NA;
        }

        internal static string ToNaString(this DateTime data)
        {
            return data.ToString(DateTimePattern);
        }

        public static string ToYesNo(this bool data)
        {
            return data ? "Yes" : "No";
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
                case 1: return "January";
                case 2: return "February";
                case 3: return "March";
                case 4: return "April";
                case 5: return "May";
                case 6: return "June";
                case 7: return "July";
                case 8: return "August";
                case 9: return "September";
                case 10: return "October";
                case 11: return "November";
                case 12: return "December";
                default: throw new NotImplementedException();
            }
        }
    }
}
