using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Application.Abstractions.Reference;

public interface IPaymentProcessingStatusService
{
    Task<PagedListResponse<PaymentProcessingStatusResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        string? code,
        CancellationToken cancellationToken = default);

    Task<PaymentProcessingStatusResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PaymentProcessingStatusResponse> CreateAsync(CreatePaymentProcessingStatusRequest request, CancellationToken cancellationToken = default);

    Task<PaymentProcessingStatusResponse> UpdateAsync(int id, UpdatePaymentProcessingStatusRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}


