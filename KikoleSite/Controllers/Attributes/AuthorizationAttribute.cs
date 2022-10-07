using System;
using KikoleSite.Models.Enums;

namespace KikoleSite.Controllers.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AuthorizationAttribute : Attribute
    {
        public UserTypes MinimalUserType { get; }

        public AuthorizationAttribute(UserTypes minimalUserType = UserTypes.StandardUser)
        {
            MinimalUserType = minimalUserType;
        }
    }
}
