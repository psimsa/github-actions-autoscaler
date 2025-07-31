# Issue: Potential Resource Leaks

## Current Problem
Some resources might not be properly disposed.

## Examples
- The `CancellationTokenSource` in `StartEphemeralContainer`
- Docker client connections

## Recommendation
Use `using` statements or ensure proper disposal of resources.

## Implementation Steps

1. Properly dispose of CancellationTokenSource:
```csharp
private async Task<bool> StartEphemeralContainer(
    string repositoryFullName,
    string containerName,
    long jobRunId)
{
    // ... existing code
    using (var cts = new CancellationTokenSource())
    {
        cts.CancelAfter(DockerServiceConstants.ContainerStartTimeoutMs);
        
        if (!await PullImageIfNotExists(cts.Token))
            return false;

        // ... rest of the method
    }
}
```

2. Ensure Docker client is properly disposed:
```csharp
public class DockerClientWrapper : IDockerClientWrapper, IDisposable
{
    private readonly DockerClient _client;
    private bool _disposed = false;

    public DockerClientWrapper(DockerClient client)
    {
        _client = client;
    }

    // ... implementation of interface methods

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _client?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
```

3. Update dependency injection to handle disposal:
```csharp
builder.Services.AddSingleton<IDockerClientWrapper>(serviceProvider =>
{
    var dockerConfig = serviceProvider.GetRequiredService<DockerClientConfiguration>();
    var client = dockerConfig.CreateClient();
    return new DockerClientWrapper(client);
});
```

4. Add finalizers for cleanup:
```csharp
~DockerClientWrapper()
{
    Dispose(false);
}
```

## Benefits
- Prevents resource leaks
- Better memory management
- Improved system stability
- Proper cleanup of resources
