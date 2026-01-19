using System.Text;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Models;
using RabbitMQ.Client;

namespace GithubActionsAutoscaler.Services;

public class RabbitMQQueueService : IQueueService, IAsyncDisposable
{
    private readonly AppConfiguration _config;
    private readonly ILogger<RabbitMQQueueService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMQQueueService(AppConfiguration config, ILogger<RabbitMQQueueService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken token)
    {
        await EnsureConnectionAsync(token);
        _logger.LogInformation("Connected to RabbitMQ queue {QueueName}", _config.RabbitQueueName);
    }

    private async Task EnsureConnectionAsync(CancellationToken token)
    {
        if (_channel != null && !_channel.IsClosed)
            return;

        try
        {
            if (_connection == null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory
                {
                    HostName = _config.RabbitHost,
                    Port = _config.RabbitPort,
                    UserName = _config.RabbitUsername,
                    Password = _config.RabbitPassword,
                };
                _connection = await factory.CreateConnectionAsync(token);
            }

            if (_channel == null || _channel.IsClosed)
            {
                _channel = await _connection.CreateChannelAsync(cancellationToken: token);
                await _channel.QueueDeclareAsync(
                    queue: _config.RabbitQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: token
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure RabbitMQ connection");
            throw;
        }
    }

    public async Task<QueueMessage?> ReceiveMessageAsync(CancellationToken token)
    {
        await EnsureConnectionAsync(token);

        var result = await _channel!.BasicGetAsync(
            _config.RabbitQueueName,
            autoAck: false,
            cancellationToken: token
        );
        if (result == null)
            return null;

        var body = Encoding.UTF8.GetString(result.Body.ToArray());
        var popReceipt = result.DeliveryTag.ToString();
        var messageId = result.BasicProperties.MessageId ?? Guid.NewGuid().ToString();

        return new QueueMessage(messageId, popReceipt, body);
    }

    public async Task DeleteMessageAsync(
        string messageId,
        string popReceipt,
        CancellationToken token
    )
    {
        await EnsureConnectionAsync(token);

        if (ulong.TryParse(popReceipt, out var deliveryTag))
        {
            await _channel!.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: token);
        }
        else
        {
            _logger.LogError("Invalid PopReceipt for RabbitMQ: {PopReceipt}", popReceipt);
        }
    }

    public async Task AbandonMessageAsync(
        string messageId,
        string popReceipt,
        CancellationToken token
    )
    {
        await EnsureConnectionAsync(token);

        if (ulong.TryParse(popReceipt, out var deliveryTag))
        {
            await _channel!.BasicNackAsync(
                deliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: token
            );
        }
        else
        {
            _logger.LogError("Invalid PopReceipt for RabbitMQ: {PopReceipt}", popReceipt);
        }
    }

    public Task<string?> PeekMessageIdAsync(CancellationToken token)
    {
        return Task.FromResult<string?>(null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.CloseAsync();
        if (_connection != null)
            await _connection.CloseAsync();

        if (_channel != null)
            await _channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }
}
