namespace KikoleSite.Dtos
{
    public class EntryDto : EntryBaseDto
    {
        public uint Id { get; set; }
        public uint PlayerId { get; set; }
        public bool IsSimulatedDate { get; set; }
    }
}
