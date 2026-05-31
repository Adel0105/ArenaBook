namespace ArenaBook.Application.Contracts.Sessions;

public sealed class SessionJoinCoinQuoteResponse
{
    public int ScheduledSessionId { get; set; }

    public decimal CoinsRequired { get; set; }

    public string CurrencyCode { get; set; } = "COIN";
}

