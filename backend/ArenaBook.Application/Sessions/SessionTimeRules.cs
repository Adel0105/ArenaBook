namespace ArenaBook.Application.Sessions;

/// <summary>
/// Pravila za planiranje i završetak termina (UTC).
/// </summary>
public static class SessionTimeRules
{
    public const double MaxDurationHours = 8;

    public const int MaxAdvanceBookingDays = 365;

    public static IReadOnlyDictionary<string, string[]> ValidateStructure(DateTime startUtc, DateTime endUtc)
    {
        var errors = new Dictionary<string, string[]>();

        if (endUtc <= startUtc)
            errors["endUtc"] = ["Kraj termina mora biti nakon početka."];

        var durationHours = (endUtc - startUtc).TotalHours;
        if (durationHours > MaxDurationHours)
        {
            errors["endUtc"] =
            [
                $"Termin ne može trajati duže od {MaxDurationHours} sati.",
            ];
        }

        return errors;
    }

    /// <param name="allowHistoricalTimes">
    /// Samo administrator (npr. demo seed kroz API) smije unijeti termin u prošlosti.
    /// </param>
    public static IReadOnlyDictionary<string, string[]> ValidateSchedulingPolicy(
        DateTime startUtc,
        DateTime endUtc,
        bool allowHistoricalTimes,
        DateTime utcNow)
    {
        var errors = new Dictionary<string, string[]>(ValidateStructure(startUtc, endUtc));

        if (!allowHistoricalTimes)
        {
            if (startUtc < utcNow)
                errors["startUtc"] = ["Početak termina mora biti u budućnosti."];

            if (endUtc < utcNow)
                errors["endUtc"] = ["Kraj termina mora biti u budućnosti."];
        }

        if (startUtc > utcNow.AddDays(MaxAdvanceBookingDays))
        {
            errors["startUtc"] =
            [
                $"Termin ne može biti zakazan više od {MaxAdvanceBookingDays} dana unaprijed.",
            ];
        }

        return errors;
    }

    public static bool CanMarkCompleted(DateTime endUtc, DateTime utcNow) => endUtc <= utcNow;
}
