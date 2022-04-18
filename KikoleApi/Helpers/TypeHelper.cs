using System.Linq;

namespace KikoleApi.Helpers
{
    internal static class TypeHelper
    {
        internal static object GetSortValue<T>(this T instance, string propertyName)
        {
            return typeof(T)
                .GetProperties(System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public)
                .FirstOrDefault(p => p.Name.Equals(propertyName, System.StringComparison.InvariantCultureIgnoreCase))?
                .GetValue(instance);
        }
    }
}
