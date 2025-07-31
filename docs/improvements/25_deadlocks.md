# Issue: Potential Deadlocks

## Current Problem
The `WaitForAvailableRunner` could potentially cause deadlocks.

## Recommendation
Consider using a more sophisticated runner management approach.

## Implementation Steps

1. Create a runner manager service:
```csharp
public interface IRunnerManager
{
    Task<bool> TryAcquireRunnerSlotAsync();
    void ReleaseRunnerSlot();
    Task WaitForAvailableRunnerAsync();
}

public class RunnerManager : IRunnerManager
{
    private readonly SemaphoreSlim _runnerSemaphore;
    private readonly int _maxRunners;

    public RunnerManager(int maxRunners)
    {
        _maxRunners = maxRunners;
        _runnerSemaphore = new SemaphoreSlim(maxRunners, maxRunners);
    }

    public async Task<bool> TryAcquireRunnerSlotAsync()
    {
        return await _runnerSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
    }

    public void ReleaseRunnerSlot()
    {
        _runnerSemaphore.Release();
    }

    public async Task WaitForAvailableRunnerAsync()
    {
        await _runnerSemaphore.WaitAsync();
    }
}
```

2. Update DockerService to use the runner manager:
```csharp
public class DockerService : IDockerService
{
    private readonly IRunnerManager _runnerManager;

    public DockerService(
        IDockerClientWrapper dockerClient,
        AppConfiguration configuration,
        ILogger<DockerService> logger,
        IRunnerManager runnerManager)
    {
        _dockerClient = dockerClient;
        _logger = logger;
        _runnerManager = runnerManager;
        // ... initialization ...
    }

    public async Task<bool> StartEphemeralContainerAsync(
        string repositoryFullName,
        string containerName,
        long jobRunId)
    {
        if (!await _runnerManager.TryAcquireRunnerSlotAsync())
        {
            _logger.LogWarning("No available runner slots");
            return false;
        }

        try
        {
            // ... container creation logic ...
            return true;
        }
        finally
        {
            _runnerManager.ReleaseRunnerSlot();
        }
    }
}
```

3. Update dependency injection:
```csharp
builder.Services.AddSingleton<IRunnerManager>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<AppConfiguration>();
    return new RunnerManager(config.MaxRunners);
});
builder.Services.AddSingleton<IDockerService, DockerService>();
```

4. Add timeout to runner acquisition:
```csharp
public async Task<bool> TryAcquireRunnerSlotAsync(TimeSpan timeout)
{
    return await _runnerSemaphore.WaitAsync(timeout);
}
```

5. Add cancellation support:
```csharp
public async Task<bool> TryAcquireRunnerSlotAsync(
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    return await _runnerSemaphore.WaitAsync(timeout, cancellationToken);
}
```

6. Add runner slot monitoring:
```csharp
public class RunnerManager : IRunnerManager
{
    private readonly SemaphoreSlim _runnerSemaphore;
    private readonly int _maxRunners;
    private readonly ILogger<RunnerManager> _logger;

    public RunnerManager(int maxRunners, ILogger<RunnerManager> logger)
    {
        _maxRunners = maxRunners;
        _logger = logger;
        _runnerSemaphore = new SemaphoreSlim(maxRunners, maxRunners);
    }

    public async Task<bool> TryAcquireRunnerSlotAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var acquired = await _runnerSemaphore.WaitAsync(timeout, cancellationToken);
        if (acquired)
        {
            _logger.LogInformation(
                "Acquired runner slot. Available: {Available}/{Max}",
                _runnerSemaphore.CurrentCount, _maxRunners);
        }
        else
        {
            _logger.LogWarning(
                "Failed to acquire runner slot. Available: {Available}/{Max}",
                _runnerSemaphore.CurrentCount, _maxRunners);
        }
        return acquired;
    }
}
```

## Benefits
- Prevents deadlocks
- Better runner management
- Improved resource utilization
- More reliable container creation
- Better monitoring of runner slots
