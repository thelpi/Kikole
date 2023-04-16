using KikoleSite.Dtos;
using KikoleSite.Enums;
using KikoleSite.Extensions;

namespace KikoleSite.Models
{
    public class RankingEntryLight : Ranking
    {
        public static readonly int UnsetTimeValueSeconds = 20 * 60;

        public Game Game { get; }
        public Player Player { get; }
        public int Points { get; private set; }
        public int CumuledTime { get; private set; }
        public int UntiedRecordsCount { get; private set; }
        public int RecordsCount { get; private set; }

        public string PlayerName => Game == Game.GoldenEye ? Player?.RealName : Player?.SurName;

        internal RankingEntryLight(Game game, PlayerDto player)
        {
            Game = game;
            Player = new Player(player);

            Points = 0;
            UntiedRecordsCount = 0;
            RecordsCount = 0;

            CumuledTime = (UnsetTimeValueSeconds * Game.GetStages().Count) * 3;
        }

        internal virtual void AddStageAndLevelData(RankingDto ranking, RankingEntryDto rankingEntry, bool untied)
        {
            if (rankingEntry.Rank == 1)
            {
                RecordsCount++;
                if (untied)
                    UntiedRecordsCount++;
            }

            Points += rankingEntry.Points;

            if (rankingEntry.Time < UnsetTimeValueSeconds)
                CumuledTime -= UnsetTimeValueSeconds - rankingEntry.Time;
        }
    }
}
