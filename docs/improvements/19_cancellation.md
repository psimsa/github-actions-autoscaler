# Issue: Inconsistent Cancellation

## Current Problem
Cancellation tokens are used inconsistently.

## Recommendation
Ensure all long-running operations properly support cancellation.

## Implementation Steps

1. Create a cancellation token provider:
```csharp
public interface ICancellationTokenProvider
{
    CancellationToken Token { get; }
}

public class CancellationTokenProvider : ICancellationTokenProvider
{
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public CancellationToken Token => _cts.Token;

    public void Cancel()
    {
        _cts.Cancel();
    }
}
```

2. Update services to use cancellation tokens:
```csharp
public class DockerService : IDockerService
{
    private readonly ICancellationTokenProvider _cancellationTokenProvider;

    public DockerService(
        IDockerClientWrapper dockerClient,
        AppConfiguration configuration,
        ILogger<DockerService> logger,
        ICancellationTokenProvider cancellationTokenProvider)
    {
        _dockerClient = dockerClient;
        _logger = logger;
        _cancellationTokenProvider = cancellationTokenProvider;
        // ... initialization ...
    }

    public async Task<bool> ProcessWorkflowAsync(Workflow? workflow)
    {
        var token = _cancellationTokenProvider.Token;
        token.ThrowIfCancellationRequested();
        
        // ... implementation using token ...
    }
}
```

3. Update long-running operations:
```csharp
private async Task ContainerGuardAsync(CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        try
        {
            var containers = await GetAutoscalerContainersAsync();
            // ... process containers ...
            await Task.Delay(DockerServiceConstants.ContainerGuardInterval, token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Container guard cancelled");
            break;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in container guard");
            await Task.Delay(DockerServiceConstants.ContainerGuardErrorDelay, token);
        }
    }
}
```

4. Update dependency injection:
```csharp
builder.Services.AddSingleton<ICancellationTokenProvider, CancellationTokenProvider>();
builder.Services.AddSingleton<IDockerService, DockerService>();
```

5. Update hosted services:
```csharp
public class QueueMonitorWorker : IHostedService
{
    private readonly ICancellationTokenProvider _cancellationTokenProvider;
    private Task _workerTask;

    public QueueMonitorWorker(
        ICancellationTokenProvider cancellationTokenProvider,
        // ... other dependencies ...
    )
    {
        _cancellationTokenProvider = cancellationTokenProvider;
        // ... initialization ...
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _workerTask = MonitorQueueAsync(_cancellationTokenProvider.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenProvider.Cancel();
        if (_workerTask != null)
        {
            await _workerTask;
        }
    }
}
```

## Benefits
- Consistent cancellation handling
- Better resource cleanup
- Improved system responsiveness
- Easier shutdown handling
- More reliable long-running operations
