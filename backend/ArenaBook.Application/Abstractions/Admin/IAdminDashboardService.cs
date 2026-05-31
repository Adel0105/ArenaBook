using ArenaBook.Application.Contracts.Admin;

namespace ArenaBook.Application.Abstractions.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<AdminDashboardActivityResponse> GetActivityAsync(int months, CancellationToken cancellationToken = default);
}

