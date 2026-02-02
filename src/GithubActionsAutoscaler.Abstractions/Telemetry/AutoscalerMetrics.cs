using System.Diagnostics.Metrics;
using System.Threading;

namespace GithubActionsAutoscaler.Abstractions.Telemetry;

public class AutoscalerMetrics
{
	public const string MeterName = "GithubActionsAutoscaler";

	private readonly Counter<long> _jobsReceived;
	private readonly Counter<long> _jobsStarted;
	private readonly Counter<long> _jobsCompleted;
	private readonly Counter<long> _queueMessagesDeleted;
	private readonly Counter<long> _queueMessagesFailed;
	private long _queueDepth;
	private long _activeRunners;

	public AutoscalerMetrics(Meter meter)
	{
		_jobsReceived = meter.CreateCounter<long>("autoscaler.workflow.jobs.received");
		_jobsStarted = meter.CreateCounter<long>("autoscaler.workflow.jobs.started");
		_jobsCompleted = meter.CreateCounter<long>("autoscaler.workflow.jobs.completed");
		_queueMessagesDeleted = meter.CreateCounter<long>("autoscaler.queue.messages.deleted");
		_queueMessagesFailed = meter.CreateCounter<long>("autoscaler.queue.messages.failed");

		meter.CreateObservableGauge(
			"autoscaler.queue.depth",
			() => new Measurement<long>(Interlocked.Read(ref _queueDepth))
		);
		meter.CreateObservableGauge(
			"autoscaler.runners.active",
			() => new Measurement<long>(Interlocked.Read(ref _activeRunners))
		);
	}

	public void RecordJobReceived(string action, string mode)
	{
		_jobsReceived.Add(1, new KeyValuePair<string, object?>("action", action), new("mode", mode));
	}

	public void RecordJobStarted(string repository)
	{
		_jobsStarted.Add(1, new KeyValuePair<string, object?>("repository", repository));
	}

	public void RecordJobCompleted(string repository)
	{
		_jobsCompleted.Add(1, new KeyValuePair<string, object?>("repository", repository));
	}

	public void RecordQueueMessageDeleted()
	{
		_queueMessagesDeleted.Add(1);
	}

	public void RecordQueueMessageFailed()
	{
		_queueMessagesFailed.Add(1);
	}

	public void UpdateQueueDepth(long value)
	{
		Interlocked.Exchange(ref _queueDepth, value);
	}

	public void UpdateActiveRunners(long value)
	{
		Interlocked.Exchange(ref _activeRunners, value);
	}
}
