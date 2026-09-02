namespace KikoleSite.Models.Dtos
{
    public class UserDto : BaseDto
    {
        public required string Login { get; set; }

        public required string Password { get; set; }

        public required string PasswordResetQuestion { get; set; }

        public required string PasswordResetAnswer { get; set; }

        public ulong LanguageId { get; set; }

        public ulong UserTypeId { get; set; }

        public string? Ip { get; set; }
    }
}
