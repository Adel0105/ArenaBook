namespace ArenaBook.Application.Abstractions;

public interface IDemoDataSeedService
{
    Task<DemoDataSeedResult> SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class DemoDataSeedResult
{
    public int CitiesEnsured { get; init; }

    public int HallsCreated { get; init; }

    public int PlayersCreated { get; init; }

    public int SessionsCreated { get; init; }

    public int PaymentsCreated { get; init; }
}

