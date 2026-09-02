namespace KikoleSite.Models.Dtos
{
    public class PlayersDistributionDto<T>
    {
        public required T Value { get; set; }

        public int Count { get; set; }
    }
}
