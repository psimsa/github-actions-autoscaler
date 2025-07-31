# Issue: Potential Race Conditions

## Current Problem
The `_containerGuardTask` might have race conditions.

## Recommendation
Use proper synchronization mechanisms to prevent race conditions.

## Implementation Steps

1. Use a lock object for synchronization:
```csharp
private readonly object _containerGuardLock = new object();
private Task _containerGuardTask;
```

2. Update the container guard task creation:
```csharp
private void EnsureContainerGuardRunning()
{
    lock (_containerGuardLock)
    {
        if (_containerGuardTask == null || _containerGuardTask.IsCompleted)
        {
            _containerGuardTask = ContainerGuard(CancellationToken.None);
        }
    }
}
```

3. Use the synchronization method:
```csharp
public async Task<bool> ProcessWorkflow(Workflow? workflow)
{
    // ... existing code ...
    
    if (workflow?.Action == "queued" && CheckIfRepoIsWhitelistedOrHasAllowedPrefix(workflow.Repository.FullName))
    {
        // ... existing code ...
        
        var result = await StartEphemeralContainer(
            workflow.Repository.FullName,
            containerName,
            workflow.Job.RunId
        );
        
        if (result)
        {
            EnsureContainerGuardRunning();
        }
        
        return result;
    }
    
    // ... rest of the method ...
}
```

4. Add synchronization to other critical sections:
```csharp
private async Task<bool> StartEphemeralContainer(
    string repositoryFullName,
    string containerName,
    long jobRunId)
{
    lock (_containerGuardLock)
    {
        if ((await GetAutoscalerContainersAsync()).Count >= _maxRunners)
        {
            return false;
        }
    }
    
    // ... rest of the method ...
}
```

5. Consider using async locks for async operations:
```csharp
private readonly SemaphoreSlim _containerGuardSemaphore = new SemaphoreSlim(1, 1);

private async Task EnsureContainerGuardRunningAsync()
{
    await _containerGuardSemaphore.WaitAsync();
    try
    {
        if (_containerGuardTask == null || _containerGuardTask.IsCompleted)
        {
            _containerGuardTask = ContainerGuard(CancellationToken.None);
        }
    }
    finally
    {
        _containerGuardSemaphore.Release();
    }
}
```

## Benefits
- Prevents race conditions
- Improves thread safety
- More reliable container management
- Better system stability
