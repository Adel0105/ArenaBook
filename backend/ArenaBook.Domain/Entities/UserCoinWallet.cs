namespace ArenaBook.Domain.Entities;

public sealed class UserCoinWallet
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public decimal BalanceCoins { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public ICollection<CoinLedgerEntry> LedgerEntries { get; set; } = new List<CoinLedgerEntry>();
}

