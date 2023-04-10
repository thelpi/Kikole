using System.Collections.Generic;

namespace KikoleSite.Extensions
{
    public static class ViewExtensions
    {
        private static readonly IReadOnlyDictionary<int, string> RankSuffixes =
            new Dictionary<int, string>
            {
                { 1, "st" },
                { 2, "nd" },
                { 3, "rd" }
            };

        public static string ToRankString(this int rank)
        {
            return RankSuffixes.ContainsKey(rank)
                ? $"{rank}{RankSuffixes[rank]}"
                : $"{rank}th";
        }
    }
}
