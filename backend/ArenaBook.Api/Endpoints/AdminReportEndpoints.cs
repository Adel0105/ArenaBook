using ArenaBook.Application.Abstractions.Admin;
using ArenaBook.Application.Authorization;

namespace ArenaBook.Api.Endpoints;

public static class AdminReportEndpoints
{
    public static WebApplication MapAdminReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/reports")
            .RequireAuthorization(AuthPolicies.AdministratorOnly)
            .WithTags("Admin - Reports");

        group.MapGet("/sessions/pdf", SessionsPdfAsync);
        group.MapGet("/transactions/pdf", TransactionsPdfAsync);
        group.MapGet("/users/pdf", UsersPdfAsync);

        return app;
    }

    private static async Task<IResult> SessionsPdfAsync(
        IAdminReportService service,
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken)
    {
        var bytes = await service.GenerateSessionsReportPdfAsync(dateFromUtc, dateToUtc, cancellationToken);
        return PdfFile(bytes, "arena-book-rezervacije.pdf");
    }

    private static async Task<IResult> TransactionsPdfAsync(
        IAdminReportService service,
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken)
    {
        var bytes = await service.GenerateTransactionsReportPdfAsync(dateFromUtc, dateToUtc, cancellationToken);
        return PdfFile(bytes, "arena-book-transakcije.pdf");
    }

    private static async Task<IResult> UsersPdfAsync(
        IAdminReportService service,
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken)
    {
        var bytes = await service.GenerateUsersReportPdfAsync(dateFromUtc, dateToUtc, cancellationToken);
        return PdfFile(bytes, "arena-book-korisnici.pdf");
    }

    private static IResult PdfFile(byte[] bytes, string fileName) =>
        Results.File(bytes, "application/pdf", fileName);
}

