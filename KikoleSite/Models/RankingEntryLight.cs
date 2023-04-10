using KikoleSite.Dtos;
using KikoleSite.Enums;
using KikoleSite.Extensions;

namespace KikoleSite.Models
{
    public class RankingEntryLight : Ranking
    {
        public static readonly long UnsetTimeValueSeconds = 20 * 60;

        public Game Game { get; }
        public Player Player { get; }
        public int Points { get; private set; }
        public long CumuledTime { get; private set; }
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

        internal virtual int AddStageAndLevelDatas(RankingDto ranking, bool untied)
        {
            var points = (100 - ranking.Rank) - 2;
            if (points < 0)
            {
                points = 0;
            }
            if (ranking.Rank == 1)
            {
                points = 100;
                RecordsCount++;
                if (untied)
                {
                    UntiedRecordsCount++;
                }
            }
            else if (ranking.Rank == 2)
            {
                points = 97;
            }

            Points += points;

            if (ranking.Time < UnsetTimeValueSeconds)
            {
                CumuledTime -= UnsetTimeValueSeconds - ranking.Time;
            }

            return points;
        }
    }
}
