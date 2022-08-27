using System;

namespace KikoleSite.Elite.Models
{
    public class Ranking
    {
        public int Rank { get; private set; }
        public int SubRank { get; private set; }

        internal virtual void SetRank<T, TValue>(
            T previousRankingEntry,
            Func<T, TValue> getComparedValue)
            where T : Ranking
            where TValue : IEquatable<TValue>
        {
            Rank = 1;
            if (previousRankingEntry != null)
            {
                SubRank = previousRankingEntry.SubRank + 1;
                Rank = previousRankingEntry.Rank;
                if (!getComparedValue(previousRankingEntry).Equals(getComparedValue((T)this)))
                {
                    Rank += SubRank;
                    SubRank = 0;
                }
            }
        }
    }
}
