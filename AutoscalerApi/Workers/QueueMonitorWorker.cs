using System.Text.Json;
using System.Threading;
using AutoscalerApi.Models;
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

    private Task? worker;

    public QueueMonitorWorker(
        AppConfiguration configuration,
        IDockerService dockerService,
        ILogger<QueueMonitorWorker> logger
    )
    {
        _dockerService = dockerService;
        _logger = logger;
        _connectionString = configuration.AzureStorage;
        _queueName = configuration.AzureStorageQueue;
    }

    private async Task MonitorQueue(CancellationToken token)
    {
        _logger.LogInformation("QueueMonitorWorker is starting");
        var client = new QueueClient(_connectionString, _queueName);

        await client.CreateIfNotExistsAsync(cancellationToken: token);
        var lastUnsuccessfulMessageId = "";

        while (!token.IsCancellationRequested)
        {
            QueueMessage message = null!;
            try
            {
                await _dockerService.WaitForAvailableRunner();

                if (lastUnsuccessfulMessageId != "")
                {
                    PeekedMessage pms = await client.PeekMessageAsync(token);
                    if (pms?.MessageId == lastUnsuccessfulMessageId)
                    {
                        await Task.Delay(10_000, token);
                    }
                }

                lastUnsuccessfulMessageId = "";

                message = await client.ReceiveMessageAsync(cancellationToken: token);

                if (message == null)
                {
                    await Task.Delay(10_000, token);
                    continue;
                }

                _logger.LogInformation("Dequeued message");

                var msg = Convert.FromBase64String(message.MessageText);

                var workflow = JsonSerializer.Deserialize<Workflow>(msg);
                _logger.LogInformation("Executing workflow");
                var workflowResult = await _dockerService.ProcessWorkflow(workflow);

                if (workflowResult)
                    await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, token);
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
                    await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, token);
                }

                await Task.Delay(10_000, token);
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        worker = MonitorQueue(cancellationToken);
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueMonitorWorker is stopping");

        if (worker != null)
        {
            await worker;
        }
    }
}
