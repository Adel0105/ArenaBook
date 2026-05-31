using ArenaBook.Application.Abstractions.Admin;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ArenaBook.Infrastructure.Services.Admin;

public sealed class AdminReportService : IAdminReportService
{
    private readonly ArenaBookDbContext _db;

    public AdminReportService(ArenaBookDbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateSessionsReportPdfAsync(
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken = default) =>
        BuildPdfAsync(
            "Izvještaj o rezervacijama (terminima)",
            dateFromUtc,
            dateToUtc,
            async ct =>
            {
                var q = _db.ScheduledSessions.AsNoTracking();
                if (dateFromUtc.HasValue)
                    q = q.Where(s => s.StartUtc >= dateFromUtc.Value);
                if (dateToUtc.HasValue)
                    q = q.Where(s => s.StartUtc <= dateToUtc.Value);

                var rows = await (
                    from s in q
                    join h in _db.Halls.AsNoTracking() on s.HallId equals h.Id
                    join st in _db.SessionLifecycleStatuses.AsNoTracking() on s.SessionLifecycleStatusId equals st.Id
                    join sk in _db.SessionKinds.AsNoTracking() on s.SessionKindId equals sk.Id
                    join u in _db.Users.AsNoTracking() on s.OrganizerUserId equals u.Id
                    orderby s.StartUtc descending
                    select new SessionReportRow
                    {
                        Id = s.Id,
                        HallName = h.Name,
                        OrganizerEmail = u.Email ?? "",
                        StatusCode = st.Code,
                        KindCode = sk.Code,
                        StartUtc = s.StartUtc,
                        EndUtc = s.EndUtc,
                        MaxParticipants = s.MaxParticipants,
                        ParticipantCount = s.Participants.Count,
                    }).Take(500).ToListAsync(ct);

                var headers = new[]
                {
                    "ID", "Dvorana", "Organizator", "Status", "Vrsta", "Početak", "Kraj", "Kapacitet", "Učesnika",
                };
                var tableRows = rows.Select(r => new[]
                {
                    r.Id.ToString(),
                    r.HallName,
                    r.OrganizerEmail,
                    r.StatusCode,
                    r.KindCode,
                    FormatUtc(r.StartUtc),
                    FormatUtc(r.EndUtc),
                    r.MaxParticipants.ToString(),
                    r.ParticipantCount.ToString(),
                }).ToList();

                return (rows.Count, headers, tableRows);
            },
            cancellationToken);

    public Task<byte[]> GenerateTransactionsReportPdfAsync(
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken = default) =>
        BuildPdfAsync(
            "Izvještaj o transakcijama i uplatama",
            dateFromUtc,
            dateToUtc,
            async ct =>
            {
                var q = _db.ExternalPaymentRecords.AsNoTracking();
                if (dateFromUtc.HasValue)
                    q = q.Where(p => p.CreatedUtc >= dateFromUtc.Value);
                if (dateToUtc.HasValue)
                    q = q.Where(p => p.CreatedUtc <= dateToUtc.Value);

                var rows = await (
                    from p in q
                    join u in _db.Users.AsNoTracking() on p.UserId equals u.Id
                    join st in _db.PaymentProcessingStatuses.AsNoTracking() on p.PaymentProcessingStatusId equals st.Id
                    orderby p.CreatedUtc descending
                    select new PaymentReportRow
                    {
                        Id = p.Id,
                        UserEmail = u.Email ?? "",
                        PurposeCode = p.PurposeCode,
                        Provider = p.Provider,
                        AmountMoney = p.AmountMoney,
                        Currency = p.Currency,
                        StatusCode = st.Code,
                        CoinsPurchased = p.CoinsPurchased,
                        ExternalReference = p.ExternalReference,
                        CreatedUtc = p.CreatedUtc,
                    }).Take(500).ToListAsync(ct);

                var headers = new[]
                {
                    "ID", "Korisnik", "Svrha", "Provajder", "Iznos", "Valuta", "Status", "Koini", "Ref.", "Datum",
                };
                var tableRows = rows.Select(r => new[]
                {
                    r.Id.ToString(),
                    r.UserEmail,
                    r.PurposeCode,
                    r.Provider,
                    r.AmountMoney.ToString("0.00"),
                    r.Currency,
                    r.StatusCode,
                    r.CoinsPurchased.ToString("0.##"),
                    r.ExternalReference ?? "-",
                    FormatUtc(r.CreatedUtc),
                }).ToList();

                return (rows.Count, headers, tableRows);
            },
            cancellationToken);

    public Task<byte[]> GenerateUsersReportPdfAsync(
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken = default) =>
        BuildPdfAsync(
            "Izvještaj o korisnicima",
            dateFromUtc,
            dateToUtc,
            async ct =>
            {
                var baseQ =
                    from u in _db.Users.AsNoTracking()
                    join w in _db.UserCoinWallets.AsNoTracking() on u.Id equals w.UserId into wg
                    from w in wg.DefaultIfEmpty()
                    select new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        RegisteredUtc = w != null ? (DateTime?)w.UpdatedUtc : null,
                        u.LockoutEnd,
                    };

                if (dateFromUtc.HasValue)
                    baseQ = baseQ.Where(x => x.RegisteredUtc != null && x.RegisteredUtc >= dateFromUtc.Value);
                if (dateToUtc.HasValue)
                    baseQ = baseQ.Where(x => x.RegisteredUtc != null && x.RegisteredUtc <= dateToUtc.Value);

                var users = await baseQ
                    .OrderByDescending(x => x.RegisteredUtc)
                    .Take(500)
                    .ToListAsync(ct);

                var userIds = users.Select(u => u.Id).ToList();
                var roles = await (
                    from ur in _db.UserRoles.AsNoTracking()
                    join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                    where userIds.Contains(ur.UserId)
                    select new { ur.UserId, RoleName = r.Name })
                    .ToListAsync(ct);

                var roleLookup = roles
                    .GroupBy(x => x.UserId)
                    .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.RoleName ?? "")));

                var headers = new[] { "Email", "Ime", "Prezime", "Uloge", "Registracija", "Status" };
                var tableRows = users.Select(u => new[]
                {
                    u.Email ?? "",
                    u.FirstName ?? "",
                    u.LastName ?? "",
                    roleLookup.TryGetValue(u.Id, out var rn) ? rn : "",
                    u.RegisteredUtc.HasValue ? FormatUtc(u.RegisteredUtc.Value) : "-",
                    u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow ? "Zaključan" : "Aktivan",
                }).ToList();

                return (users.Count, headers, tableRows);
            },
            cancellationToken);

    private async Task<byte[]> BuildPdfAsync(
        string title,
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        Func<CancellationToken, Task<(int RowCount, string[] Headers, List<string[]> Rows)>> loadAsync,
        CancellationToken cancellationToken)
    {
        var (rowCount, headers, rows) = await loadAsync(cancellationToken);
        var period = FormatPeriod(dateFromUtc, dateToUtc);
        var generatedAt = DateTime.UtcNow;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("ArenaBook").Bold().FontSize(16);
                    col.Item().Text(title).SemiBold().FontSize(13);
                    col.Item().Text($"Period: {period}").FontColor(Colors.Grey.Darken2);
                    col.Item().Text($"Generisano (UTC): {FormatUtc(generatedAt)}").FontColor(Colors.Grey.Darken2);
                    col.Item().Text($"Broj zapisa: {rowCount}").FontColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingVertical(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < headers.Length; i++)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                .Text(h).SemiBold();
                        }
                    });

                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .Padding(4).Text(cell);
                        }
                    }

                    if (rows.Count == 0)
                    {
                        table.Cell().ColumnSpan((uint)headers.Length).Padding(12)
                            .Text("Nema zapisa za odabrani period.");
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Stranica ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatPeriod(DateTime? from, DateTime? to)
    {
        if (!from.HasValue && !to.HasValue)
            return "Svi podaci";
        if (from.HasValue && to.HasValue)
            return $"{FormatUtc(from.Value)} – {FormatUtc(to.Value)}";
        if (from.HasValue)
            return $"Od {FormatUtc(from.Value)}";
        return $"Do {FormatUtc(to!.Value)}";
    }

    private static string FormatUtc(DateTime dt) =>
        dt.ToUniversalTime().ToString("dd.MM.yyyy HH:mm");

    private sealed class SessionReportRow
    {
        public int Id { get; init; }
        public string HallName { get; init; } = "";
        public string OrganizerEmail { get; init; } = "";
        public string StatusCode { get; init; } = "";
        public string KindCode { get; init; } = "";
        public DateTime StartUtc { get; init; }
        public DateTime EndUtc { get; init; }
        public int MaxParticipants { get; init; }
        public int ParticipantCount { get; init; }
    }

    private sealed class PaymentReportRow
    {
        public int Id { get; init; }
        public string UserEmail { get; init; } = "";
        public string PurposeCode { get; init; } = "";
        public string Provider { get; init; } = "";
        public decimal AmountMoney { get; init; }
        public string Currency { get; init; } = "";
        public string StatusCode { get; init; } = "";
        public decimal CoinsPurchased { get; init; }
        public string? ExternalReference { get; init; }
        public DateTime CreatedUtc { get; init; }
    }
}

