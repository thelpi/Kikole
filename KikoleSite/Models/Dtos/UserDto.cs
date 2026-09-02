namespace KikoleSite.Models.Dtos
{
    public class UserDto : BaseDto
    {
        public string Login { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string PasswordResetQuestion { get; set; } = null!;

        public string PasswordResetAnswer { get; set; } = null!;

        public ulong LanguageId { get; set; }

        public ulong UserTypeId { get; set; }

        public string? Ip { get; set; }
    }
}
