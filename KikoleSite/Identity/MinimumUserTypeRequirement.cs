using KikoleSite.Models.Enums;
using Microsoft.AspNetCore.Authorization;

namespace KikoleSite.Identity;

/// <summary>
/// « Au moins ce palier » : reproduit la semantique de l'ancien
/// <c>(ulong)UserType &gt;= (ulong)minimalType</c>, une policy par palier.
/// </summary>
public class MinimumUserTypeRequirement : IAuthorizationRequirement
{
    public UserTypes MinimalType { get; }

    public MinimumUserTypeRequirement(UserTypes minimalType)
    {
        MinimalType = minimalType;
    }

    public static string PolicyName(UserTypes minimalType) => $"MinUserType_{(ulong)minimalType}";
}
