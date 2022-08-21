namespace KikoleSite.Elite
{
    public class PlayerDto
    {
        public long Id { get; set; }
        public string UrlName { get; set; }
        public string RealName { get; set; }
        public string SurName { get; set; }
        public string ControlStyle { get; set; }
        public string Color { get; set; }
        public bool IsDirty { get; set; }
        public bool IsBanned { get; set; }
    }
}
