namespace KikoleSite.Models.Dtos
{
    public record PlayersDistributionDto<T>
    {
        public required T Value { get; init; }

        public int Count { get; init; }
    }
}
