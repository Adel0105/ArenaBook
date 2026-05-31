namespace ArenaBook.Domain.Entities;

public sealed class CoinLedgerEntry
{
    public int Id { get; set; }

    public int UserCoinWalletId { get; set; }

    public UserCoinWallet UserCoinWallet { get; set; } = null!;

    public decimal AmountCoins { get; set; }

    public decimal BalanceAfter { get; set; }

    public string ReasonCode { get; set; } = string.Empty;

    public int? RelatedScheduledSessionId { get; set; }

    public ScheduledSession? RelatedScheduledSession { get; set; }

    public DateTime CreatedUtc { get; set; }
}

