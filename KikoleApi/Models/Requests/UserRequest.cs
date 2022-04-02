using System;
using KikoleApi.Helpers;
using KikoleApi.Interfaces;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Models.Requests
{
    public class UserRequest
    {
        public string Login { get; set; }

        public string Password { get; set; }

        public string PasswordResetQuestion { get; set; }

        public string PasswordResetAnswer { get; set; }

        public Languages? Language { get; set; }

        internal string IsValid()
        {
            if (string.IsNullOrWhiteSpace(Login))
                return SPA.TextResources.InvalidLogin;

            if (string.IsNullOrWhiteSpace(Password))
                return SPA.TextResources.InvalidPassword;

            if (Language.HasValue && !Enum.IsDefined(typeof(Languages), Language.Value))
                return SPA.TextResources.InvalidLanguage;

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
                LanguageId = (ulong)(Language ?? Languages.en),
                Login = Login.Sanitize(),
                Password = crypter.Encrypt(Password),
                PasswordResetAnswer = crypter.Encrypt(realPasswordResetAnswer),
                PasswordResetQuestion = realPasswordResetQuestion,
                UserTypeId = (ulong)UserTypes.StandardUser
            };
        }
    }
}
