namespace ArenaBook.Application.Contracts.Reference;

public sealed class UpdatePaymentProcessingStatusRequest
{
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}


