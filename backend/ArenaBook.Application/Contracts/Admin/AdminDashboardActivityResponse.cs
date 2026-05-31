namespace ArenaBook.Application.Contracts.Admin;

public sealed class AdminDashboardActivityResponse
{
    public IReadOnlyList<AdminDashboardMonthlyPoint> UsersByMonth { get; set; } = Array.Empty<AdminDashboardMonthlyPoint>();

    public IReadOnlyList<AdminDashboardMonthlyPoint> SessionsByMonth { get; set; } = Array.Empty<AdminDashboardMonthlyPoint>();

    public IReadOnlyList<AdminDashboardMonthlyPoint> PaymentsByMonth { get; set; } = Array.Empty<AdminDashboardMonthlyPoint>();
}

public sealed class AdminDashboardMonthlyPoint
{
    public int Year { get; set; }

    public int Month { get; set; }

    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}

