using ArenaBook.Application.Common.Exceptions;

namespace ArenaBook.Application.Sessions;

/// <summary>
/// Dozvoljeni prelazi:
/// PENDING → CONFIRMED | CANCELLED
/// CONFIRMED → COMPLETED | CANCELLED
/// COMPLETED i CANCELLED su terminalni.
/// </summary>
public static class ScheduledSessionLifecycleStateMachine
{
    public sealed class TransitionPlan
    {
        public required string TargetStatusCode { get; init; }

        public bool RefundParticipants { get; init; }
    }

    public static TransitionPlan Plan(
        string fromStatusCode,
        ScheduledSessionLifecycleAction action,
        DateTime endUtc,
        DateTime utcNow,
        string? cancelReason)
    {
        if (action == ScheduledSessionLifecycleAction.Cancel
            && string.IsNullOrWhiteSpace(cancelReason))
        {
            throw new ValidationException(
                "Validacija nije prošla.",
                new Dictionary<string, string[]>
                {
                    ["reason"] = ["Razlog otkazivanja je obavezan."],
                });
        }

        var from = NormalizeCode(fromStatusCode);

        if (from is SessionLifecycleCodes.Cancelled or SessionLifecycleCodes.Completed)
        {
            throw new ConflictException("Termin je već završen ili otkazan.");
        }

        return (from, action) switch
        {
            (SessionLifecycleCodes.Pending, ScheduledSessionLifecycleAction.Confirm) => new TransitionPlan
            {
                TargetStatusCode = SessionLifecycleCodes.Confirmed,
            },
            (SessionLifecycleCodes.Pending, ScheduledSessionLifecycleAction.Cancel) => new TransitionPlan
            {
                TargetStatusCode = SessionLifecycleCodes.Cancelled,
                RefundParticipants = true,
            },
            (SessionLifecycleCodes.Pending, ScheduledSessionLifecycleAction.AdminDelete) => new TransitionPlan
            {
                TargetStatusCode = SessionLifecycleCodes.Cancelled,
                RefundParticipants = true,
            },
            (SessionLifecycleCodes.Confirmed, ScheduledSessionLifecycleAction.Cancel) => new TransitionPlan
            {
                TargetStatusCode = SessionLifecycleCodes.Cancelled,
                RefundParticipants = true,
            },
            (SessionLifecycleCodes.Confirmed, ScheduledSessionLifecycleAction.AdminDelete) => new TransitionPlan
            {
                TargetStatusCode = SessionLifecycleCodes.Cancelled,
                RefundParticipants = true,
            },
            (SessionLifecycleCodes.Confirmed, ScheduledSessionLifecycleAction.Complete) => PlanComplete(endUtc, utcNow),
            _ => throw new ConflictException(DescribeIllegalTransition(from, action)),
        };
    }

    private static TransitionPlan PlanComplete(DateTime endUtc, DateTime utcNow)
    {
        if (!SessionTimeRules.CanMarkCompleted(endUtc, utcNow))
        {
            throw new ConflictException(
                "Termin se može označiti kao završen tek nakon planiranog kraja (EndUtc).");
        }

        return new TransitionPlan
        {
            TargetStatusCode = SessionLifecycleCodes.Completed,
        };
    }

    private static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();

    private static string DescribeIllegalTransition(string from, ScheduledSessionLifecycleAction action) =>
        action switch
        {
            ScheduledSessionLifecycleAction.Confirm =>
                "Samo termin na čekanju (PENDING) može biti potvrđen.",
            ScheduledSessionLifecycleAction.Complete =>
                "Samo potvrđen termin (CONFIRMED) može biti označen kao završen.",
            ScheduledSessionLifecycleAction.Cancel or ScheduledSessionLifecycleAction.AdminDelete =>
                "Termin se može otkazati samo iz statusa PENDING ili CONFIRMED.",
            _ => "Nedozvoljen lifecycle prelaz.",
        };
}
