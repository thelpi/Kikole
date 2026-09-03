using System;
using KikoleSite.Helpers;
using KikoleSite.Identity;
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

    /// <summary>
    /// La reponse de securite n'est pas hachee ici : ce record n'a pas a connaitre
    /// l'algorithme de hachage. L'appelant la hache lui-meme avant de creer le compte.
    /// </summary>
    internal (ApplicationUser User, string RawPasswordResetAnswer) ToApplicationUser()
    {
        // ni question ni reponse fournies : un GUID sert de valeur inutilisable, pour
        // qu'un compte cree sans Q&A ne reste pas avec un secret devinable ou vide.
        var rawPasswordResetAnswer = string.IsNullOrWhiteSpace(PasswordResetAnswer)
            ? Guid.NewGuid().ToString()
            : PasswordResetAnswer.Sanitize();

        var realPasswordResetQuestion = string.IsNullOrWhiteSpace(PasswordResetQuestion)
            ? Guid.NewGuid().ToString()
            : PasswordResetQuestion;

        var user = new ApplicationUser
        {
            UserName = Login.Sanitize(),
            LanguageId = (ulong)(Language ?? Languages.en),
            UserType = UserTypes.StandardUser,
            PasswordResetQuestion = realPasswordResetQuestion,
            PasswordResetAnswerHash = string.Empty,
            Ip = Ip
        };

        return (user, rawPasswordResetAnswer);
    }
}
