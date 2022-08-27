namespace KikoleSite.Elite.Dtos
{
    public class EntryWebDto : EntryBaseDto
    {
        public string PlayerUrlName { get; set; }
        public string EngineUrl { get; set; }

        internal EntryDto ToEntry(long playerId)
        {
            return new EntryDto
            {
                PlayerId = playerId,
                Stage = Stage,
                Level = Level,
                Date = Date,
                Time = Time,
                Engine = Engine
            };
        }

        public override string ToString()
        {
            return $"[player:{PlayerUrlName}|stage:{Stage}|level:{Level}|time:{Time}]";
        }
    }
}
