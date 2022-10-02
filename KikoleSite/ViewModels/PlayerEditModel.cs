namespace KikoleSite.ViewModels
{
    public class PlayerEditModel
    {
        public ulong PlayerId { get; set; }
        public string ClueEn { get; set; }
        public string EasyClueEn { get; set; }
        public string ClueFr { get; set; }
        public string EasyClueFr { get; set; }
        public bool? Success { get; set; }
        public string Message { get; set; }
    }
}
