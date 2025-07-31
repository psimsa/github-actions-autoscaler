# Issue: Potential Memory Leaks

## Current Problem
The `_autoscalerContainersDefinition` is never cleared.

## Recommendation
Consider implementing a cleanup mechanism.

## Implementation Steps

1. Update the container definition to use a more efficient structure:
```csharp
private readonly Dictionary<string, IDictionary<string, bool>> _autoscalerContainersDefinition =
    new Dictionary<string, IDictionary<string, bool>>
    {
        {
            "label",
            new Dictionary<string, bool> { { "autoscaler", true } }
        }
    };
```

2. Implement a cleanup mechanism:
```csharp
public class DockerService : IDockerService, IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _autoscalerContainersDefinition.Clear();
                // Dispose other resources
            }
            _disposed = true;
        }
    }
}
```

3. Add a cleanup method for containers:
```csharp
public async Task CleanupContainersAsync()
{
    var containers = await GetAutoscalerContainersAsync();
    foreach (var container in containers)
    {
        try
        {
            await _client.Containers.StopContainerAsync(
                container.ID,
                new ContainerStopParameters { WaitBeforeKillSeconds = 20 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerId}", container.ID);
        }
    }
}
```

4. Update the container guard to handle cleanup:
```csharp
private async Task ContainerGuardAsync(CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        try
        {
            var containers = await GetAutoscalerContainersAsync();
            if (!containers.Any())
            {
                await Task.Delay(DockerServiceConstants.ContainerGuardInterval, token);
                continue;
            }

            foreach (var container in containers)
            {
                if (container.Status.Equals("created", StringComparison.OrdinalIgnoreCase))
                {
                    await _client.Containers.StartContainerAsync(
                        container.ID,
                        new ContainerStartParameters(),
                        token);
                }
                else if (HasContainerExpired(container))
                {
                    await _client.Containers.StopContainerAsync(
                        container.ID,
                        new ContainerStopParameters { WaitBeforeKillSeconds = 20 });
                }
            }

            await Task.Delay(DockerServiceConstants.ContainerGuardInterval, token);
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error in container guard");
            await Task.Delay(DockerServiceConstants.ContainerGuardErrorDelay, token);
        }
    }
}
```

5. Add a finalizer for cleanup:
```csharp
~DockerService()
{
    Dispose(false);
}
```

## Benefits
- Prevents memory leaks
- Better resource management
- Improved system stability
- Proper cleanup of resources
- More efficient container management
