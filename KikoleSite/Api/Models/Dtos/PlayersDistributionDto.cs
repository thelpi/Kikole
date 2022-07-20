namespace KikoleSite.Api.Models.Dtos
{
    public class PlayersDistributionDto<T>
    {
        public T Value { get; set; }

        public int Count { get; set; }
    }
}
