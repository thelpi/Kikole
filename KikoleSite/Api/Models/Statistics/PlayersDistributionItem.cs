namespace KikoleSite.Api.Models.Statistics
{
    public class PlayersDistributionItem<T>
    {
        public T Value { get; }

        public int Count { get; }

        public decimal Rate { get; }

        public int Rank { get; internal set; }

        public PlayersDistributionItem(T value, int count, int totalCount)
        {
            Value = value;
            Count = count;
            Rate = count / (decimal)totalCount * 100;
        }
    }
}
