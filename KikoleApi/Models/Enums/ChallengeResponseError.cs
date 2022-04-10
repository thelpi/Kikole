namespace KikoleApi.Models.Enums
{
    public enum ChallengeResponseError
    {
        NoError,
        InvalidChallengeId,
        InvalidUser,
        ChallengeNotFound,
        CantAutoAcceptChallenge,
        BothAcceptedAndCancelledChallenge,
        ChallengeAlreadyAccepted,
        ChallengeAlreadyAnswered,
        InvalidOpponentAccount,
        ChallengeHostIsInvalid,
        ChallengeCreatorIsAdmin,
        ChallengeOpponentIsInvalid,
        ChallengeOpponentIsAdmin,
        ChallengeAlreadyExist
    }
}
