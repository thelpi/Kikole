using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KikoleApi.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KikoleApi.Helpers
{
    public class Crypter : ICrypter
    {
        const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        const int GenerateWordSize = 16;

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
    }
}
