namespace ArenaBook.Application.Contracts.Admin;

public sealed class AdminDashboardSummaryResponse
{
    public int TotalUsers { get; set; }

    public int ActiveSessionsCount { get; set; }

    public int TotalHalls { get; set; }

    public int ExternalPaymentsCount { get; set; }
}

