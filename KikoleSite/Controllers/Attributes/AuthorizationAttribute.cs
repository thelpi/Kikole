using System;
using KikoleSite.Identity;
using KikoleSite.Models.Enums;
using Microsoft.AspNetCore.Authorization;

namespace KikoleSite.Controllers.Attributes;

/// <summary>
/// Exige d'etre connecte avec au moins le palier utilisateur donne. S'appuie sur le
/// pipeline d'autorisation standard (<see cref="AuthorizeAttribute"/>) : la policy est
/// resolue par <see cref="MinimumUserTypeRequirement.PolicyName"/>, enregistree pour
/// chaque palier au demarrage.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AuthorizationAttribute : AuthorizeAttribute
{
    public UserTypes MinimalUserType { get; }

    public AuthorizationAttribute(UserTypes minimalUserType = UserTypes.StandardUser)
    {
        MinimalUserType = minimalUserType;
        Policy = MinimumUserTypeRequirement.PolicyName(minimalUserType);
    }
}
