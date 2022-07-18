using System;

namespace KikoleSite.Api.Models.Dtos
{
    public class PlayersDistributionDto<T>
    {
        public T Value { get; set; }

        public int Count { get; set; }

        public PlayersDistributionDto<Target> ToTarget<Target>(Func<T, Target> transform)
        {
            return new PlayersDistributionDto<Target>
            {
                Value = transform(Value),
                Count = Count
            };
        }
    }
}
