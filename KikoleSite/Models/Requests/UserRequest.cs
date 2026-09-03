using KikoleSite.Helpers;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models.Requests
{
    public class UserRequest
    {
        public required string Login { get; set; }

        public required string Password { get; set; }

        public required string? PasswordResetQuestion { get; set; }

        public required string? PasswordResetAnswer { get; set; }

        public Languages? Language { get; set; }

        public required string? Ip { get; set; }

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
