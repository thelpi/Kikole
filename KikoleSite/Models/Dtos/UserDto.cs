using System;

namespace KikoleSite.Models.Dtos
{
    public record UserDto : BaseDto
    {
        public required string Login { get; init; }

        public required string NormalizedLogin { get; init; }

        public required string Password { get; init; }

        public required string PasswordResetQuestion { get; init; }

        public required string PasswordResetAnswer { get; init; }

        public ulong LanguageId { get; init; }

        public ulong UserTypeId { get; init; }

        public string? Ip { get; init; }

        public bool IsDisabled { get; init; }

        public required string ConcurrencyStamp { get; init; }

        public required string SecurityStamp { get; init; }

        public DateTime? LockoutEnd { get; init; }

        public int AccessFailedCount { get; init; }

        public bool LockoutEnabled { get; init; }
    }
}
