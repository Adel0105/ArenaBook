namespace ArenaBook.Application.Contracts.Coins;

public sealed class CoinWalletAdminListItemResponse
{
    public int WalletId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string? UserEmail { get; set; }

    public decimal BalanceCoins { get; set; }

    public DateTime UpdatedUtc { get; set; }
}

