namespace KikoleApi.Models.Dtos
{
    public class UserDto : BaseDto
    {
        public string Login { get; set; }

        public string Password { get; set; }

        public string PasswordResetQuestion { get; set; }

        public string PasswordResetAnswer { get; set; }

        public ulong LanguageId { get; set; }

        public ulong UserTypeId { get; set; }
    }
}
