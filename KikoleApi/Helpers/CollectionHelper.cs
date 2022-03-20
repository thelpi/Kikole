using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleApi.Helpers
{
    internal static class CollectionHelper
    {
        internal static List<TList> SetPositions<TList, T>(
            this IEnumerable<TList> leaders,
            Func<TList, T> keyFunc,
            bool descending,
            Action<TList, int> setPosFunc)
            where T : struct
        {
            var orderedLeaders = descending
                ? leaders.OrderByDescending(keyFunc)
                : leaders.OrderBy(keyFunc);

            T? currentValue = null;
            int currentPos = 0;
            return orderedLeaders
                .Select((l, i) =>
                {
                    if (!currentValue.HasValue || !currentValue.Value.Equals(keyFunc(l)))
                    {
                        currentPos = i + 1;
                        currentValue = keyFunc(l);
                    }
                    setPosFunc(l, currentPos);
                    return l;
                })
                .ToList();
        }
    }
}
