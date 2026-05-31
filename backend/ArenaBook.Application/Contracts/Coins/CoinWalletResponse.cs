namespace ArenaBook.Application.Contracts.Coins;

public sealed class CoinWalletResponse
{
    public string UserId { get; set; } = string.Empty;

    public decimal BalanceCoins { get; set; }

    public DateTime? UpdatedUtc { get; set; }

    public bool WalletExists { get; set; }
}

