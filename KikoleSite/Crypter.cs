using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace KikoleSite
{
    public class Crypter : ICrypter
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int GenerateWordSize = 16;

        private readonly string _cookieEncryptionKey;
        private readonly string _encryptionKey;
        private readonly Random _randomizer;
        private readonly SHA256 _sha256;
        private readonly Encoding _encoding;

        public Crypter(IConfiguration configuration)
        {
            _encryptionKey = configuration.GetValue<string>("EncryptionKey");
            _randomizer = new Random();
            _sha256 = SHA256.Create();
            _encoding = Encoding.UTF8;
            _cookieEncryptionKey = configuration.GetValue<string>("EncryptionCookieKey");
        }

        public string Encrypt(string data)
        {
            var sb = new StringBuilder();

            var hashBytes = _sha256.ComputeHash(_encoding.GetBytes($"{data.Trim()}{_encryptionKey}"));
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        public string Generate()
        {
            return new string(
                Enumerable
                    .Range(0, GenerateWordSize)
                    .Select(_ => Alphabet[_randomizer.Next(0, Alphabet.Length)])
                    .ToArray());
        }

        public string EncryptCookie(string plainText)
        {
            try
            {
                byte[] array;
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_cookieEncryptionKey);
                    aes.IV = new byte[16];
                    var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    using var memoryStream = new MemoryStream();
                    using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                    using (var streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(plainText);
                    }
                    array = memoryStream.ToArray();
                }
                return Convert.ToBase64String(array);
            }
            catch
            {
                // TODO: log
                return plainText;
            }
        }

        public string DecryptCookie(string encryptedText)
        {
            try
            {
                var buffer = Convert.FromBase64String(encryptedText);
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_cookieEncryptionKey);
                aes.IV = new byte[16];
                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var memoryStream = new MemoryStream(buffer);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);
                return streamReader.ReadToEnd();
            }
            catch
            {
                // TODO: log
                return encryptedText;
            }
        }
    }
}
