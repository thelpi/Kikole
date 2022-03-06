using System;

namespace KikoleApi.Controllers.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AuthenticationLevelAttribute : Attribute
    {
        public AuthenticationLevel Level { get; }

        public AuthenticationLevelAttribute(AuthenticationLevel level)
        {
            Level = level;
        }
    }
}
