namespace KikoleSite.Models
{
    public class AccountModel
    {
        public bool IsAuthenticated { get; set; }

        public string Login { get; set; }

        public string LoginSubmission { get; set; }
        public string Password1Submission { get; set; }
        public string Password2Submission { get; set; }

        public string Error { get; set; }
    }
}
