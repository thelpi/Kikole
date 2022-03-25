using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Challenge
    {
        public string OpponentLogin { get; }

        public byte PointsRate { get; }

        public bool? IsAccepted { get; }

        internal Challenge(ChallengeDto dto, string login)
        {
            OpponentLogin = login;
            PointsRate = dto.PointsRate;
            IsAccepted = dto.IsAccepted.HasValue
                ? dto.IsAccepted > 0
                : default(bool?);
        }
    }
}
