using System.Diagnostics;
using System.Text.Json;
using GithubActionsAutoscaler.Abstractions.Models;
using GithubActionsAutoscaler.Abstractions.Queue;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Abstractions.Telemetry;

namespace GithubActionsAutoscaler.Workers;

public class QueueMonitorWorker : IHostedService
{
	private readonly IWorkflowProcessor _workflowProcessor;
	private readonly ActivitySource _activitySource;
	private readonly ILogger<QueueMonitorWorker> _logger;
	private readonly IQueueProvider _queueProvider;
	private readonly AutoscalerMetrics? _metrics;
	private readonly string _mode;
	private string _lastUnsuccessfulMessageId = "";

	private Task? _worker;
	private CancellationTokenSource? _cts;

	public QueueMonitorWorker(
		IQueueProvider queueProvider,
		IWorkflowProcessor workflowProcessor,
		ActivitySource activitySource,
		ILogger<QueueMonitorWorker> logger,
		AutoscalerMetrics? metrics = null
	)
	{
		_queueProvider = queueProvider;
		_workflowProcessor = workflowProcessor;
		this._activitySource = activitySource;
		_logger = logger;
		_metrics = metrics;
		_mode = "QueueMonitor";
	}

	internal async Task ProcessNextMessageAsync(CancellationToken token)
	{
		using var activity = _activitySource.StartActivity();
		IQueueMessage? message = null;
		try
		{
			// Initialize done during startup
			if (_lastUnsuccessfulMessageId != "")
			{
				IQueueMessage? pms = await _queueProvider.PeekMessageAsync(token);
				if (pms?.MessageId == _lastUnsuccessfulMessageId)
				{
					await Task.Delay(10_000, token);
				}
			}

			_lastUnsuccessfulMessageId = "";

			message = await _queueProvider.ReceiveMessageAsync(token);
			_metrics?.UpdateQueueDepth(await _queueProvider.GetApproximateMessageCountAsync(token));

			if (message == null)
			{
				Activity.Current?.Stop();
				await Task.Delay(10_000, token);
				return;
			}

			activity?.AddEvent(new ActivityEvent("Dequeued message"));

			var msg = Convert.FromBase64String(message.Content);

			var workflow = JsonSerializer.Deserialize<Workflow>(msg);
			if (workflow != null)
			{
				_metrics?.RecordJobReceived(workflow.Action ?? "unknown", _mode);
			}
			activity?.AddEvent(new ActivityEvent("Executing workflow"));
			var workflowResult = await _workflowProcessor.ProcessWorkflowAsync(workflow);

			if (workflowResult)
			{
				await _queueProvider.DeleteMessageAsync(message, token);
				_metrics?.RecordQueueMessageDeleted();
			}
			else
			{
				_lastUnsuccessfulMessageId = message.MessageId;
				_metrics?.RecordQueueMessageFailed();
			}
			
			_metrics?.UpdateQueueDepth(await _queueProvider.GetApproximateMessageCountAsync(token));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error receiving message");
			activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);

			if (message != null)
			{
				activity?.AddEvent(new ActivityEvent("Deleting message due to processing error"));
				// In case of processing error (serialization etc), we delete to avoid poison pill
				// But we might want to reconsider this strategy later (dead letter queue?)
				await _queueProvider.DeleteMessageAsync(message, token);
				_metrics?.RecordQueueMessageDeleted();
				_metrics?.UpdateQueueDepth(await _queueProvider.GetApproximateMessageCountAsync(token));
			}
			activity?.Stop();
			await Task.Delay(10_000, token);
		}
	}

	private async Task MonitorQueueAsync(CancellationToken token)
	{
		_logger.LogInformation("QueueMonitorWorker monitor loop starting");

		using (var activity = _activitySource.StartActivity("QueueMonitorWorker.Startup"))
		{
			await _queueProvider.InitializeAsync(token);
		}

		while (!token.IsCancellationRequested)
		{
			await ProcessNextMessageAsync(token);
		}
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("QueueMonitorWorker is starting");
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
