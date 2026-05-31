namespace ArenaBook.Domain.Entities;

public sealed class PaymentProcessingStatus
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ICollection<ExternalPaymentRecord> ExternalPayments { get; set; } = new List<ExternalPaymentRecord>();
}

