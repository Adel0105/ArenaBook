namespace ArenaBook.Application.Contracts.Reference;

public sealed class PaymentProcessingStatusResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}


