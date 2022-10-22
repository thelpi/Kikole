namespace KikoleSite.Models.Enums
{
    public static class Extensions
    {
        public static bool CanBeMiss(this ProposalTypes proposalType)
        {
            return proposalType != ProposalTypes.Clue
                && proposalType != ProposalTypes.Leaderboard;
        }
    }
}
