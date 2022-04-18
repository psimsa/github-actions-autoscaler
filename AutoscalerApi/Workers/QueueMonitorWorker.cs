using System.Text.Json;
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

    public QueueMonitorWorker(AppConfiguration configuration, IDockerService dockerService,
        ILogger<QueueMonitorWorker> logger)
    {
        _dockerService = dockerService;
        _logger = logger;
        _connectionString = configuration.AzureStorage;
        _queueName = configuration.AzureStorageQueue;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueMonitorWorker is starting");
        var client = new QueueClient(_connectionString, _queueName);

        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var lastUnsuccessfulMessageId = "";

        while (!cancellationToken.IsCancellationRequested)
        {
            QueueMessage message = null!;
            try
            {
                await _dockerService.WaitForAvailableRunner();

                if (lastUnsuccessfulMessageId != "")
                {
                    PeekedMessage pms = await client.PeekMessageAsync(cancellationToken);
                    if (pms?.MessageId == lastUnsuccessfulMessageId)
                    {
                        await Task.Delay(10_000, cancellationToken);
                    }
                }

                lastUnsuccessfulMessageId = "";

                message = await client.ReceiveMessageAsync(cancellationToken: cancellationToken);

                if (message == null)
                {
                    await Task.Delay(10_000, cancellationToken);
                    continue;
                }

                _logger.LogInformation("Dequeued message");

                var msg = Convert.FromBase64String(message.MessageText);

                var workflow = JsonSerializer.Deserialize(msg,
                    Models.ApplicationJsonSerializerContext.Default.Workflow);
                _logger.LogInformation("Executing workflow");
                var workflowResult = await _dockerService.ProcessWorkflow(workflow);

                if (workflowResult)
                    await client.DeleteMessageAsync(message.MessageId, message.PopReceipt,
                        cancellationToken);
                else
                {
                    lastUnsuccessfulMessageId = message.MessageId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving message");
                if (message != null)
                {
                    await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
                }

                await Task.Delay(10_000, cancellationToken);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueMonitorWorker is stopping");
    }
}
