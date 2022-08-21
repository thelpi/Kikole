using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite
{
    public static class SystemExtensions
    {
        public static IEnumerable<T> Enumerate<T>() where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("The targeted type is not an enum.");
            }

            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static IEnumerable<DateTime> LoopBetweenDates(this DateTime startDate, DateStep stepType, Api.Interfaces.IClock clock)
        {
            return LoopBetweenDates(startDate, clock.Now, stepType, 1);
        }

        public static DateTime Latest(this DateTime date1, DateTime date2)
        {
            return date1 > date2 ? date1 : date2;
        }

        public static IEnumerable<DateTime> LoopBetweenDates(this DateTime startDate, DateTime endDate, DateStep stepType)
        {
            return LoopBetweenDates(startDate, endDate, stepType, 1);
        }

        public static IEnumerable<DateTime> LoopBetweenDates(this DateTime startDate, DateTime endDate, DateStep stepType, int stepValue)
        {
            for (DateTime date = startDate.Truncat(stepType); date <= endDate.Truncat(stepType); date = _dateStepDelegates[stepType](date, stepValue))
            {
                yield return date;
            }
        }

        public static DateTime Truncat(this DateTime dateTime, DateStep stepType)
        {
            switch (stepType)
            {
                case DateStep.Second:
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
                case DateStep.Minute:
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);
                case DateStep.Hour:
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);
                case DateStep.Day:
                case DateStep.Week:
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind);
                case DateStep.Month:
                    return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);
                case DateStep.Year:
                    return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
                default:
                    return dateTime;
            }
        }

        private static readonly IReadOnlyDictionary<DateStep, Func<DateTime, int, DateTime>> _dateStepDelegates =
            new Dictionary<DateStep, Func<DateTime, int, DateTime>>
            {
                { DateStep.Second, (d, i) => d.AddSeconds(i) },
                { DateStep.Minute, (d, i) => d.AddMinutes(i) },
                { DateStep.Hour, (d, i) => d.AddHours(i) },
                { DateStep.Day, (d, i) => d.AddDays(i) },
                { DateStep.Week, (d, i) => d.AddDays(i * 7) },
                { DateStep.Month, (d, i) => d.AddMonths(i) },
                { DateStep.Year, (d, i) => d.AddYears(i) }
            };

        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static List<T> IntersectOrConcat<T>(
            this List<T> items,
            IEnumerable<T> secondItems)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (secondItems == null)
            {
                throw new ArgumentNullException(nameof(secondItems));
            }

            return (items.Count == 0
                    ? items.Concat(secondItems)
                    : items.Intersect(secondItems))
                .ToList();
        }

        public static void SetRank<T>(this IReadOnlyList<T> items,
            Func<T, T, bool> equalityComparer,
            Func<T, int> getRankFunc,
            Action<T, int> setRankFunc)
        {
            var counter = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (i == 0)
                {
                    setRankFunc(items[i], 1);
                }
                else
                {
                    if (equalityComparer(items[i - 1], items[i]))
                    {
                        setRankFunc(items[i], getRankFunc(items[i - 1]));
                        counter++;
                    }
                    else
                    {
                        setRankFunc(items[i], getRankFunc(items[i - 1]) + counter + 1);
                        counter = 0;
                    }
                }
            }
        }

        public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> toAdd)
        {
            if (bag == null)
                throw new ArgumentNullException(nameof(bag));

            if (toAdd == null)
                throw new ArgumentNullException(nameof(toAdd));

            foreach (var element in toAdd)
            {
                bag.Add(element);
            }
        }
    }
}
