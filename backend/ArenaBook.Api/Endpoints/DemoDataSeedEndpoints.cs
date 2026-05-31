using ArenaBook.Application.Abstractions;

namespace ArenaBook.Api.Endpoints;

public static class DemoDataSeedEndpoints
{
    public static WebApplication MapDemoDataSeedEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapPost("/api/dev/seed-demo-data", SeedDemoDataAsync)
            .AllowAnonymous()
            .WithTags("Dev - Demo seed")
            .WithSummary("Puni ili dopunjuje demo podatke (gradovi BiH, dvorane, igrači, termini, transakcije).");

        return app;
    }

    private static async Task<IResult> SeedDemoDataAsync(
        IDemoDataSeedService seedService,
        CancellationToken cancellationToken)
    {
        var result = await seedService.SeedAsync(cancellationToken);
        return Results.Ok(new
        {
            message = "Demo podaci su uspješno primijenjeni (idempotentno).",
            result.CitiesEnsured,
            result.HallsCreated,
            result.PlayersCreated,
            result.SessionsCreated,
            result.PaymentsCreated,
        });
    }
}

