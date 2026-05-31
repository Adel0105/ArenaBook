using System.Text;
using ArenaBook.Application.Abstractions.Messaging;
using ArenaBook.Application.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ArenaBook.Infrastructure.Services.Messaging;

public sealed class RabbitMqEventPublisher : IRabbitMqEventPublisher, IAsyncDisposable
{
    private const string QueueName = "arena-book.events";

    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task PublishJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            return;

        var channel = await GetOrCreateChannelAsync(cancellationToken);
        var body = Encoding.UTF8.GetBytes(json);
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            mandatory: false,
            basicProperties: new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent },
            body: body,
            cancellationToken: cancellationToken);
    }

    private async Task<IChannel> GetOrCreateChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            if (_connection is not { IsOpen: true })
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    VirtualHost = string.IsNullOrEmpty(_options.VirtualHost) ? "/" : _options.VirtualHost,
                };
                _connection = await factory.CreateConnectionAsync(cancellationToken);
            }

            _channel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);
            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
