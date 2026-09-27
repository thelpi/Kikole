using System;
using System.Threading;
using System.Threading.Tasks;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using KikoleSite.Repositories;
using Microsoft.AspNetCore.Identity;

namespace KikoleSite.Identity;

/// <summary>
/// Store Identity adosse a <see cref="IUserRepository"/> (Dapper/MySqlConnector), pour
/// rester sur le seul acces aux donnees du projet plutot que d'introduire EF Core.
///
/// Seules les interfaces reellement utilisees sont implementees : ni telephone, ni 2FA,
/// ni roles/claims/logins externes (aucun de ces mecanismes n'est employe ici, le niveau
/// utilisateur passe par une claim <see cref="UserTypes"/> geree ailleurs).
/// </summary>
public class DapperUserStore :
    IUserStore<ApplicationUser>,
    IUserPasswordStore<ApplicationUser>,
    IUserLockoutStore<ApplicationUser>,
    IUserSecurityStampStore<ApplicationUser>,
    IUserEmailStore<ApplicationUser>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailProtector _emailProtector;

    public DapperUserStore(IUserRepository userRepository, IEmailProtector emailProtector)
    {
        _userRepository = userRepository;
        _emailProtector = emailProtector;
    }

    // ------------------------------------------------------------------ IUserStore

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var id = await _userRepository.CreateUserAsync(ToDto(user, 0));
        user.Id = id;
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        await _userRepository.UpdateUserAsync(ToDto(user, user.Id));
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        await _userRepository.DeleteUserAsync(user.Id);
        return IdentityResult.Success;
    }

    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!ulong.TryParse(userId, out var id))
            return null;

        var dto = await _userRepository.GetUserByIdAsync(id);
        return dto == null ? null : ToUser(dto);
    }

    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var dto = await _userRepository.GetUserByNormalizedLoginAsync(normalizedUserName);
        return dto == null ? null : ToUser(dto);
    }

    // ------------------------------------------------------------------ IUserPasswordStore

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    // ------------------------------------------------------------------ IUserLockoutStore

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.LockoutEnabled);

    public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------------ IUserSecurityStampStore

    public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.SecurityStamp);

    // ------------------------------------------------------------------ IUserEmailStore

    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var hash = _emailProtector.Hash(normalizedEmail);
        var dto = await _userRepository.GetUserByEmailHashAsync(hash);
        return dto == null ? null : ToUser(dto);
    }

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------------

    public void Dispose()
    {
        // rien a liberer : IUserRepository gere son propre cycle de vie de connexion.
    }

    private UserDto ToDto(ApplicationUser user, ulong id)
    {
        var email = user.Email ?? string.Empty;

        return new UserDto
        {
            Id = id,
            Login = user.UserName ?? string.Empty,
            NormalizedLogin = user.NormalizedUserName ?? string.Empty,
            Password = user.PasswordHash ?? string.Empty,
            EmailEncrypted = _emailProtector.Encrypt(email),
            EmailHash = _emailProtector.Hash(email),
            EmailConfirmed = user.EmailConfirmed,
            LanguageId = user.LanguageId,
            UserTypeId = (ulong)user.UserType,
            Ip = user.Ip,
            IsDisabled = user.IsDisabled,
            ConcurrencyStamp = user.ConcurrencyStamp ?? Guid.NewGuid().ToString(),
            SecurityStamp = user.SecurityStamp ?? Guid.NewGuid().ToString(),
            LockoutEnd = user.LockoutEnd?.UtcDateTime,
            AccessFailedCount = user.AccessFailedCount,
            LockoutEnabled = user.LockoutEnabled,
            SponsorUserId = user.SponsorUserId
        };
    }

    private ApplicationUser ToUser(UserDto dto)
    {
        return new ApplicationUser
        {
            Id = dto.Id,
            UserName = dto.Login,
            NormalizedUserName = dto.NormalizedLogin,
            PasswordHash = dto.Password,
            Email = _emailProtector.Decrypt(dto.EmailEncrypted),
            EmailConfirmed = dto.EmailConfirmed,
            LanguageId = dto.LanguageId,
            UserType = (UserTypes)dto.UserTypeId,
            Ip = dto.Ip,
            IsDisabled = dto.IsDisabled,
            ConcurrencyStamp = dto.ConcurrencyStamp,
            SecurityStamp = dto.SecurityStamp,
            LockoutEnd = dto.LockoutEnd.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(dto.LockoutEnd.Value, DateTimeKind.Utc))
                : null,
            AccessFailedCount = dto.AccessFailedCount,
            LockoutEnabled = dto.LockoutEnabled,
            CreationDate = dto.CreationDate,
            SponsorUserId = dto.SponsorUserId
        };
    }
}
