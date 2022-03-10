namespace KikoleApi.Models
{
    public class ProposalResponse
    {
        private int _lostPoints;

        public bool Successful { get; set; }

        public object Value { get; set; }

        public string Tip { get; set; }

        public int LostPoints
        {
            get { return _lostPoints; }
            set
            {
                _lostPoints = value;
                TotalPoints -= _lostPoints;
                TotalPoints = System.Math.Max(TotalPoints, 0);
            }
        }

        public int TotalPoints { get; internal set; }
    }
}
