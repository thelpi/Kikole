using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Api.Helpers
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

        internal static List<TList> SetPositions<TList, T1, T2>(
            this IEnumerable<TList> leaders,
            Func<TList, T1> keyFunc,
            Func<TList, T2> thenBykeyFunc,
            bool descending,
            bool thenDescending,
            Action<TList, int> setPosFunc)
            where T1 : struct
            where T2 : struct
        {
            var orderedLeaders = descending
                ? leaders.OrderByDescending(keyFunc)
                : leaders.OrderBy(keyFunc);

            orderedLeaders = thenDescending
                ? orderedLeaders.ThenBy(thenBykeyFunc)
                : orderedLeaders.ThenByDescending(thenBykeyFunc);

            T1? currentValue1 = null;
            T2? currentValue2 = null;
            int currentPos = 0;
            return orderedLeaders
                .Select((l, i) =>
                {
                    if (!currentValue1.HasValue || !currentValue1.Value.Equals(keyFunc(l)))
                    {
                        currentPos = i + 1;
                        currentValue1 = keyFunc(l);
                    }
                    else if (!currentValue2.HasValue || !currentValue2.Value.Equals(keyFunc(l)))
                    {
                        currentPos = i + 1;
                        currentValue2 = thenBykeyFunc(l);
                    }
                    setPosFunc(l, currentPos);
                    return l;
                })
                .ToList();
        }
    }
}
