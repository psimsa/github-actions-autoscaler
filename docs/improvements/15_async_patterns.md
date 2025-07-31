# Issue: Inconsistent Async Patterns

## Current Problem
Some methods use `async`/`await` inconsistently.

## Recommendation
Follow consistent async patterns throughout the codebase.

## Implementation Steps

1. Ensure all async methods follow the `Async` naming convention:
```csharp
public async Task<IList<ContainerListResponse>> GetAutoscalerContainersAsync()
{
    return await _client.Containers.ListContainersAsync(
        new ContainersListParameters() { Filters = _autoscalerContainersDefinition, All = true }
    );
}
```

2. Use `ConfigureAwait(false)` for library code:
```csharp
public async Task<bool> ProcessWorkflowAsync(Workflow? workflow)
{
    // ... implementation ...
    await SomeAsyncOperation().ConfigureAwait(false);
    // ... rest of implementation ...
}
```

3. Avoid async void methods:
```csharp
// Bad
public async void ProcessQueueMessage(QueueMessage message)
{
    // ... implementation ...
}

// Good
public async Task ProcessQueueMessageAsync(QueueMessage message)
{
    // ... implementation ...
}
```

4. Use proper cancellation tokens:
```csharp
public async Task<bool> StartEphemeralContainerAsync(
    string repositoryFullName,
    string containerName,
    long jobRunId,
    CancellationToken cancellationToken = default)
{
    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
    {
        cts.CancelAfter(DockerServiceConstants.ContainerStartTimeoutMs);
        
        // ... implementation using cts.Token ...
    }
}
```

5. Handle task exceptions properly:
```csharp
public async Task ProcessQueueMessagesAsync(CancellationToken cancellationToken)
{
    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = await _queueClient.ReceiveMessageAsync(cancellationToken);
                await ProcessQueueMessageAsync(message, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing queue message");
                await Task.Delay(DockerServiceConstants.QueueMonitorDelayMs, cancellationToken);
            }
        }
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("Queue processing cancelled");
    }
}
```

6. Use async-friendly synchronization:
```csharp
private readonly SemaphoreSlim _containerGuardSemaphore = new SemaphoreSlim(1, 1);

private async Task EnsureContainerGuardRunningAsync()
{
    await _containerGuardSemaphore.WaitAsync();
    try
    {
        if (_containerGuardTask == null || _containerGuardTask.IsCompleted)
        {
            _containerGuardTask = ContainerGuardAsync(CancellationToken.None);
        }
    }
    finally
    {
        _containerGuardSemaphore.Release();
    }
}
```

## Benefits
- Consistent async patterns
- Better error handling
- Improved performance
- More maintainable code
- Proper resource cleanup
