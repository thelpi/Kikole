namespace KikoleSite.Models.Dtos
{
    public class PlayersDistributionDto<T>
    {
        public T Value { get; set; } = default!;

        public int Count { get; set; }
    }
}
