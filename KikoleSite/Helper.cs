using System.Collections.Generic;
using System.Dynamic;

namespace KikoleSite
{
    internal static class Helper
    {
        internal static bool IsPropertyExist(dynamic settings, string name)
        {
            return settings is ExpandoObject
                ? ((IDictionary<string, object>)settings).ContainsKey(name)
                : settings.GetType().GetProperty(name) != null;
        }
    }
}
