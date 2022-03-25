using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Challenge
    {
        public string OpponentLogin { get; set; }

        public byte PointsRate { get; set; }

        public bool? IsAccepted { get; set; }

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
