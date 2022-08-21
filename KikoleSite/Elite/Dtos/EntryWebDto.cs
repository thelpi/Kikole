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
    }
}
