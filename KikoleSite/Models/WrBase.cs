using KikoleSite.Enums;

namespace KikoleSite.Models
{
    public class WrBase
    {
        public Stage Stage { get; }
        public Level Level { get; }
        public int Time { get; }

        internal WrBase(Stage stage, Level level, int time)
        {
            Stage = stage;
            Level = level;
            Time = time;
        }
    }
}
