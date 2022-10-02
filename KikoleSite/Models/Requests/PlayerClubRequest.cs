using KikoleSite.Models.Dtos;

namespace KikoleSite.Models.Requests
{
    /// <summary>
    /// Request object to link a player to a club.
    /// </summary>
    public class PlayerClubRequest
    {
        /// <summary>
        /// Position in career history (starts at 1).
        /// </summary>
        public byte HistoryPosition { get; set; }

        /// <summary>
        /// Is a loan y/n ?
        /// </summary>
        public bool IsLoan { get; set; }

        /// <summary>
        /// Club identifier.
        /// </summary>
        public ulong ClubId { get; set; }

        internal PlayerClubDto ToPlayerClubDto(ulong playerId)
        {
            return new PlayerClubDto
            {
                HistoryPosition = HistoryPosition,
                ClubId = ClubId,
                PlayerId = playerId,
                IsLoan = (byte)(IsLoan ? 1 : 0)
            };
        }
    }
}
