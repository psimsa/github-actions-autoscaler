using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using GithubActionsAutoscaler.Models;
using GithubActionsAutoscaler.Services;

namespace GithubActionsAutoscaler.Workers;

public class QueueMonitorWorker : IHostedService
{
    private readonly IDockerService _dockerService;
    private readonly ILogger<QueueMonitorWorker> _logger;
    private readonly QueueClient _queueClient;
    private string _lastUnsuccessfulMessageId = "";

    private Task? _worker;
    private CancellationTokenSource? _cts;

    public QueueMonitorWorker(
        QueueClient queueClient,
        IDockerService dockerService,
        ILogger<QueueMonitorWorker> logger
    )
    {
        _queueClient = queueClient;
        _dockerService = dockerService;
        _logger = logger;
    }

    internal async Task ProcessNextMessageAsync(CancellationToken token)
    {
        QueueMessage? message = null;
        try
        {
            await _dockerService.WaitForAvailableRunnerAsync();

            if (_lastUnsuccessfulMessageId != "")
            {
                PeekedMessage? pms = await _queueClient.PeekMessageAsync(token);
                if (pms?.MessageId == _lastUnsuccessfulMessageId)
                {
                    await Task.Delay(10_000, token);
                }
            }

            _lastUnsuccessfulMessageId = "";

            var response = await _queueClient.ReceiveMessageAsync(cancellationToken: token);
            message = response.Value;

            if (message == null)
            {
                await Task.Delay(10_000, token);
                return;
            }

            _logger.LogInformation("Dequeued message");

            var msg = Convert.FromBase64String(message.MessageText);

            var workflow = JsonSerializer.Deserialize<Workflow>(msg);
            _logger.LogInformation("Executing workflow");
            var workflowResult = await _dockerService.ProcessWorkflowAsync(workflow);

            if (workflowResult)
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, token);
            else
            {
                _lastUnsuccessfulMessageId = message.MessageId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving message");
            if (message != null)
            {
                // In case of processing error (serialization etc), we delete to avoid poison pill
                // But we might want to reconsider this strategy later (dead letter queue?)
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, token);
            }

            await Task.Delay(10_000, token);
        }
    }

    private async Task MonitorQueueAsync(CancellationToken token)
    {
        _logger.LogInformation("QueueMonitorWorker is starting");

        await _queueClient.CreateIfNotExistsAsync(cancellationToken: token);

        while (!token.IsCancellationRequested)
        {
            await ProcessNextMessageAsync(token);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _worker = MonitorQueueAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueMonitorWorker is stopping");
        _cts?.Cancel();

        if (_worker != null)
        {
            try
            {
                await _worker;
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }
    }
}
