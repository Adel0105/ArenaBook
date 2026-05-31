namespace ArenaBook.Application.Contracts.Coins;

public sealed class CoinLedgerListItemResponse
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string? UserEmail { get; set; }

    public decimal AmountCoins { get; set; }

    public decimal BalanceAfter { get; set; }

    public string ReasonCode { get; set; } = string.Empty;

    public int? RelatedScheduledSessionId { get; set; }

    public string? RelatedHallName { get; set; }

    public DateTime? RelatedSessionStartUtc { get; set; }

    public DateTime CreatedUtc { get; set; }
}

