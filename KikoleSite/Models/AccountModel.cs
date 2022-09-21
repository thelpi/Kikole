namespace KikoleSite.Models
{
    public class AccountModel
    {
        public bool IsAuthenticated { get; set; }

        public string Login { get; set; }

        public string LoginSubmission { get; set; }
        public string PasswordSubmission { get; set; }

        public string LoginCreateSubmission { get; set; }
        public string PasswordCreate1Submission { get; set; }
        public string PasswordCreate2Submission { get; set; }
        public string RecoveryQCreate { get; set; }
        public string RecoveryACreate { get; set; }

        public bool ForceLoginAction { get; set; }

        public string Error { get; set; }
        public string SuccessInfo { get; set; }


        public string LoginRecoverySubmission { get; set; }
        public string QuestionRecovery { get; set; }

        public string RegistrationId { get; set; }
    }
}
