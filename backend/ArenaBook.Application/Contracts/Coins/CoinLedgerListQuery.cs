namespace ArenaBook.Application.Contracts.Coins;

public sealed class CoinLedgerListQuery
{
    public string? UserId { get; set; }

    public string? ReasonCode { get; set; }

    public int? RelatedScheduledSessionId { get; set; }

    public DateTime? DateFromUtc { get; set; }

    public DateTime? DateToUtc { get; set; }

    public string? Q { get; set; }

    public bool ExcludeDemoSeed { get; set; }
}

