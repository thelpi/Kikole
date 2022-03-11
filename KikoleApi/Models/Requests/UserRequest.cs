using System;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models.Requests
{
    public class UserRequest
    {
        public string Login { get; set; }

        public string Password { get; set; }

        public string PasswordResetQuestion { get; set; }

        public string PasswordResetAnswer { get; set; }

        public Language? Language { get; set; }

        internal string IsValid()
        {
            if (string.IsNullOrWhiteSpace(Login))
                return "Empty login";

            if (string.IsNullOrWhiteSpace(Password))
                return "Empty password";

            if (Language.HasValue && !Enum.IsDefined(typeof(Language), Language.Value))
                return "Invalid language";

            return null;
        }

        internal UserDto ToDto(ICrypter crypter)
        {
            var realPasswordResetAnswer = string.IsNullOrWhiteSpace(PasswordResetAnswer)
                ? crypter.Generate()
                : PasswordResetAnswer.Sanitize();

            var realPasswordResetQuestion = string.IsNullOrWhiteSpace(PasswordResetQuestion)
                ? crypter.Generate()
                : PasswordResetQuestion;

            return new UserDto
            {
                LanguageId = (ulong)(Language ?? Models.Language.en),
                Login = Login.Sanitize(),
                Password = crypter.Encrypt(Password),
                PasswordResetAnswer = crypter.Encrypt(realPasswordResetAnswer),
                PasswordResetQuestion = realPasswordResetQuestion
            };
        }
    }
}
