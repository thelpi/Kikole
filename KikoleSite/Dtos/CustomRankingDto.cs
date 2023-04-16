using KikoleSite.Enums;

namespace KikoleSite.Dtos
{
    public class CustomRankingDto : BaseRankingEntryDto
    {
        public Stage Stage { get; set; }
        public Level Level { get; set; }
    }
}
