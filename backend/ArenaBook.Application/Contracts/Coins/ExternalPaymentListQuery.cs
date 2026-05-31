namespace ArenaBook.Application.Contracts.Coins;

public sealed class ExternalPaymentListQuery
{
    public string? UserId { get; set; }

    public int? PaymentProcessingStatusId { get; set; }

    public string? PurposeCode { get; set; }

    public string? Provider { get; set; }

    public DateTime? DateFromUtc { get; set; }

    public DateTime? DateToUtc { get; set; }

    public string? Q { get; set; }

    public bool ExcludeDemoSeed { get; set; }
}

