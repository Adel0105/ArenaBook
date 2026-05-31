namespace ArenaBook.Application.Abstractions.Admin;

public interface IAdminReportService
{
    Task<byte[]> GenerateSessionsReportPdfAsync(
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateTransactionsReportPdfAsync(
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateUsersReportPdfAsync(
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken = default);
}

