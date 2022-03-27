using System;
using KikoleApi.Models.Enums;

namespace KikoleApi.Controllers.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AuthenticationLevelAttribute : Attribute
    {
        public UserTypes? UserType { get; }

        public AuthenticationLevelAttribute(UserTypes userType)
        {
            UserType = userType;
        }

        public AuthenticationLevelAttribute()
        {
            UserType = null;
        }
    }
}
