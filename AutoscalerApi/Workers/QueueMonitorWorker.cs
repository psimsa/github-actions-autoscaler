using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoscalerApi.Controllers;
using AutoscalerApi.Services;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace AutoscalerApi.Workers;

public class QueueMonitorWorker : IHostedService
{
    private readonly IDockerService _dockerService;
    private readonly ILogger<QueueMonitorWorker> _logger;
    private readonly string _connectionString;
    private readonly string _queueName;
    private int _maxRunners;

    public QueueMonitorWorker(AppConfiguration configuration, IDockerService dockerService,
        ILogger<QueueMonitorWorker> logger)
    {
        _dockerService = dockerService;
        _logger = logger;
        _connectionString = configuration.AzureStorage;
        _queueName = configuration.AzureStorageQueue;
        _maxRunners = configuration.MaxRunners;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var client = new QueueClient(_connectionString, _queueName);

        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            QueueMessage message = null!;
            try
            {
                message = await client.ReceiveMessageAsync(TimeSpan.FromSeconds(10), cancellationToken);

                if (message != null)
                {
                    _logger.LogInformation("Dequeued message");
                    var decodedMessage = Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));
                    var workflow =
                        JsonSerializer.Deserialize<Workflow>(decodedMessage, AppSerizerContext.Default.Workflow);
                    if (workflow != null)
                    {
                        _logger.LogInformation($"Executing workflow");
                        await _dockerService.ProcessWorkflow(workflow);
                    }

                    await client.DeleteMessageAsync(message.MessageId, message.PopReceipt,
                        cancellationToken);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving message");
                if (message != null)
                {
                    await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}

[JsonSerializable(typeof(Workflow))]
public partial class AppSerizerContext : JsonSerializerContext
{
}