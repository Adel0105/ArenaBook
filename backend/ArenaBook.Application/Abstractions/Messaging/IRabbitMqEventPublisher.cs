namespace ArenaBook.Application.Abstractions.Messaging;

public interface IRabbitMqEventPublisher
{
    Task PublishJsonAsync(string json, CancellationToken cancellationToken = default);
}

