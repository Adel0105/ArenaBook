using System.Globalization;
using ArenaBook.Application.Abstractions.Notifications;

namespace ArenaBook.Infrastructure.Services.Sessions;

internal static class SessionNotificationBuilder
{
    public static string FormatSessionWhen(DateTime startUtc) =>
        startUtc.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) + " UTC";

    public static UserNotificationMessage SessionCreated(string userId, string hallName, DateTime startUtc) =>
        new(
            userId,
            "Termin kreiran",
            $"Vaš termin u dvorani {hallName} ({FormatSessionWhen(startUtc)}) je kreiran i čeka potvrdu.",
            "session_created");

    public static UserNotificationMessage SessionConfirmed(string userId, string hallName, DateTime startUtc) =>
        new(
            userId,
            "Termin potvrđen",
            $"Termin u dvorani {hallName} ({FormatSessionWhen(startUtc)}) je potvrđen.",
            "session_confirmed");

    public static UserNotificationMessage ParticipantJoinedOrganizer(
        string organizerUserId,
        string hallName,
        DateTime startUtc) =>
        new(
            organizerUserId,
            "Novi sudionik",
            $"Novi igrač se pridružio vašem terminu u dvorani {hallName} ({FormatSessionWhen(startUtc)}).",
            "session_participant_joined");

    public static UserNotificationMessage ParticipantJoinedSelf(string userId, string hallName, DateTime startUtc) =>
        new(
            userId,
            "Pridruživanje uspješno",
            $"Uspješno ste se pridružili terminu u dvorani {hallName} ({FormatSessionWhen(startUtc)}).",
            "session_joined");

    public static UserNotificationMessage SessionCancelled(
        string userId,
        string hallName,
        DateTime startUtc,
        string reason) =>
        new(
            userId,
            "Termin otkazan",
            $"Termin u dvorani {hallName} ({FormatSessionWhen(startUtc)}) je otkazan. Razlog: {reason}",
            "session_cancelled");

    public static UserNotificationMessage SessionDeleted(
        string userId,
        string hallName,
        DateTime startUtc,
        string reason) =>
        new(
            userId,
            "Termin uklonjen",
            $"Termin u dvorani {hallName} ({FormatSessionWhen(startUtc)}) je uklonjen od strane administratora. Razlog: {reason}",
            "session_deleted");

    public static UserNotificationMessage SessionRefund(
        string userId,
        string hallName,
        decimal amountCoins) =>
        new(
            userId,
            "Povrat koina",
            $"Vraćeno je {amountCoins.ToString("0.##", CultureInfo.InvariantCulture)} koina zbog otkazivanja termina u dvorani {hallName}.",
            "session_refund");

    public static UserNotificationMessage SessionCompleted(string userId, string hallName, DateTime startUtc) =>
        new(
            userId,
            "Termin završen",
            $"Termin u dvorani {hallName} ({FormatSessionWhen(startUtc)}) je završen.",
            "session_completed");
}
