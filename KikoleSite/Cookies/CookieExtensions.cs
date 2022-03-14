using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KikoleSite.Cookies
{
    internal static class CookieExtensions
    {
        const string _authenticationCookieName = "AccountForm";
        const string _submissionFormCookieName = "SubmissionForm";
        const string _cryptedAuthenticationCookieName = "AccountFormCrypt";
        const string _cryptedSubmissionFormCookieName = "SubmissionFormCrypt";

        const string CookiePartsSeparator = "§§§";

        static readonly DateTime SwitchDate = Startup.StaticConfig.GetValue<DateTime>("EncryptionCookieDateSwitch");
        static readonly string EncryptionKey = Startup.StaticConfig.GetValue<string>("EncryptionCookieKey");

        static bool CookiesAreCrypted => DateTime.Now >= SwitchDate;

        internal static void SetCookie(this Controller controller,
            string cookieName,
            string cookieValue,
            DateTime expiration)
        {
            if (CookiesAreCrypted)
                cookieValue = cookieValue.Encrypt();

            controller.Response.Cookies.Delete(cookieName);
            var option = new CookieOptions
            {
                Expires = expiration,
                IsEssential = true,
                Secure = false
            };
            controller.Response.Cookies.Append(cookieName, cookieValue, option);
        }

        internal static (string token, string login) GetAuthenticationCookie(this Controller controller)
        {
            var cookieValue = controller.GetCookieContent(GetAuthenticationCookieName());
            if (cookieValue != null)
            {
                var cookieParts = cookieValue.Split(CookiePartsSeparator);
                if (cookieParts.Length > 1)
                {
                    return (cookieParts[0], cookieParts[1]);
                }
            }

            return (null, null);
        }

        internal static SubmissionForm GetSubmissionFormCookie(this Controller controller)
        {
            var cookieValue = controller.GetCookieContent(GetSubmissionFormCookieName());
            if (cookieValue != null)
            {
                try
                {
                    return JsonConvert.DeserializeObject<SubmissionForm>(cookieValue);
                }
                catch
                {
                    // TODO: log
                }
            }

            return null;
        }

        internal static void SetSubmissionFormCookie(this Controller controller, SubmissionForm value)
        {
            controller.SetCookie(GetSubmissionFormCookieName(),
                JsonConvert.SerializeObject(value),
                DateTime.Now.AddDays(1).Date);
        }

        internal static void SetAuthenticationCookie(this Controller controller,
            string token,
            string login)
        {
            controller.SetCookie(GetAuthenticationCookieName(),
                $"{token}{CookiePartsSeparator}{login}",
                DateTime.Now.AddMonths(1));
        }

        internal static void ResetAuthenticationCookie(this Controller controller)
        {
            controller.Response.Cookies.Delete(GetAuthenticationCookieName());
        }

        internal static void ResetSubmissionFormCookie(this Controller controller)
        {
            controller.Response.Cookies.Delete(GetSubmissionFormCookieName());
        }

        internal static string GetIp(this Controller controller)
        {
            return controller.Request.HttpContext.Connection.RemoteIpAddress.ToString();
        }

        private static string GetCookieContent(this Controller controller,
            string cookieName)
        {
            if (!controller.Request.Cookies.TryGetValue(cookieName, out string cookieValue))
                return null;
            if (CookiesAreCrypted)
                cookieValue = cookieValue.Decrypt();
            return cookieValue;
        }

        private static string GetAuthenticationCookieName()
        {
            return CookiesAreCrypted
                ? _cryptedAuthenticationCookieName
                : _authenticationCookieName;
        }

        private static string GetSubmissionFormCookieName()
        {
            return CookiesAreCrypted
                ? _cryptedSubmissionFormCookieName
                : _submissionFormCookieName;
        }

        private static string Encrypt(this string plainText)
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

        private static string Decrypt(this string encryptedText)
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
    }
}
