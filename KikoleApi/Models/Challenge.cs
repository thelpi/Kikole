using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Challenge
    {
        public string HostLogin { get; set; }

        public byte PointsRate { get; set; }

        internal Challenge(ChallengeDto dto, string login)
        {
            HostLogin = login;
            PointsRate = dto.PointsRate;
        }
    }
}
