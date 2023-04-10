using KikoleSite.Elite.Enums;

namespace KikoleSite.Elite.Models
{
    public class WrBase
    {
        public Stage Stage { get; }
        public Level Level { get; }
        public long Time { get; }

        internal WrBase(Stage stage, Level level, long time)
        {
            Stage = stage;
            Level = level;
            Time = time;
        }
    }
}
