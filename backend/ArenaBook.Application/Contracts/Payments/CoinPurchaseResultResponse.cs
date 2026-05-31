namespace ArenaBook.Application.Contracts.Payments;

public sealed class CoinPurchaseResultResponse
{
    public decimal BalanceCoins { get; set; }

    public decimal CoinsPurchased { get; set; }
}

