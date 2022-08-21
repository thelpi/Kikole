namespace KikoleSite.Elite
{
    public class EntryDto : EntryBaseDto
    {
        public long Id { get; set; }
        public long PlayerId { get; set; }
        public bool IsSimulatedDate { get; set; }
    }
}
