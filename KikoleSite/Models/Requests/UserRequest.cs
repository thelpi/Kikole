using KikoleSite.Helpers;
using KikoleSite.Interfaces;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests
{
    public class UserRequest
    {
        public string Login { get; set; }

        public string Password { get; set; }

        public string PasswordResetQuestion { get; set; }

        public string PasswordResetAnswer { get; set; }

        public Languages? Language { get; set; }

        public string Ip { get; set; }

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
                UserTypeId = (ulong)UserTypes.StandardUser,
                Ip = Ip
            };
        }
    }
}
