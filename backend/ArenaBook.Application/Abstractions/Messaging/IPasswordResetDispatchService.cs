namespace ArenaBook.Application.Abstractions.Messaging;

public interface IPasswordResetDispatchService
{
    bool IsAvailable { get; }

    Task DispatchAsync(string email, string resetToken, CancellationToken cancellationToken = default);
}
