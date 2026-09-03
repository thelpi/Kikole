using KikoleSite.Helpers;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;

namespace KikoleSite.Models.Requests;

public record UserRequest
{
    public required string Login { get; init; }

    public required string Password { get; init; }

    public required string? PasswordResetQuestion { get; init; }

    public required string? PasswordResetAnswer { get; init; }

    public Languages? Language { get; init; }

    public required string? Ip { get; init; }

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
