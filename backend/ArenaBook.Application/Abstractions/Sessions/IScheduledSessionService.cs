using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Sessions;

namespace ArenaBook.Application.Abstractions.Sessions;

public interface IScheduledSessionService
{
    Task<PagedListResponse<ScheduledSessionListItemResponse>> GetPagedAsync(
        PageRequest page,
        ScheduledSessionListQuery query,
        CancellationToken cancellationToken = default);

    Task<ScheduledSessionDetailsResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<SessionJoinCoinQuoteResponse> GetJoinCoinQuoteAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledSessionAuditEntryResponse>> GetAuditTrailAsync(
        int sessionId,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<ScheduledSessionDetailsResponse> CreateAsync(
        CreateScheduledSessionRequest request,
        string organizerUserId,
        CancellationToken cancellationToken = default);

    Task<ScheduledSessionDetailsResponse> UpdateAsync(
        int id,
        UpdateScheduledSessionRequest request,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<ScheduledSessionDetailsResponse> ConfirmAsync(
        int id,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<ScheduledSessionDetailsResponse> CancelAsync(
        int id,
        CancelScheduledSessionRequest request,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<ScheduledSessionDetailsResponse> CompleteAsync(
        int id,
        string userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, string actorUserId, CancellationToken cancellationToken = default);

    Task JoinAsync(
        int id,
        JoinScheduledSessionRequest request,
        string userId,
        CancellationToken cancellationToken = default);
}

