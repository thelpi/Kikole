namespace KikoleSite.Models.Dtos;

public record PlayerClubDto
{
    public ulong PlayerId { get; init; }

    public ulong ClubId { get; init; }

    public byte HistoryPosition { get; init; }

    public byte IsLoan { get; init; }
}
