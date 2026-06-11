namespace ArenaBook.Application.Abstractions.Messaging;

public interface IEmailSender
{
    bool IsConfigured { get; }

    Task SendAsync(
        string toEmail,
        string subject,
        string plainTextBody,
        CancellationToken cancellationToken = default);
}
