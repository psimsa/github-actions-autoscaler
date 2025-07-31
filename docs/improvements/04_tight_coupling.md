# Issue: Tight Coupling

## Current Problem
The `DockerService` class has tight coupling with Docker.DotNet models and implementation details.

## Recommendation
Use more abstraction and dependency injection to make the service more testable and maintainable.

## Implementation Steps

1. Create an abstraction for Docker operations:
```csharp
public interface IDockerClientWrapper
{
    Task<IList<ContainerListResponse>> ListContainersAsync(
        ContainersListParameters parameters,
        CancellationToken cancellationToken = default);

    Task<CreateContainerResponse> CreateContainerAsync(
        CreateContainerParameters parameters,
        CancellationToken cancellationToken = default);

    Task<bool> StartContainerAsync(
        string containerId,
        ContainerStartParameters parameters,
        CancellationToken cancellationToken = default);

    Task<bool> StopContainerAsync(
        string containerId,
        ContainerStopParameters parameters,
        CancellationToken cancellationToken = default);

    Task RemoveContainerAsync(
        string containerId,
        ContainerRemoveParameters parameters,
        CancellationToken cancellationToken = default);

    Task<VolumesCreateResponse> CreateVolumeAsync(
        VolumesCreateParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IList<ImagesListResponse>> ListImagesAsync(
        ImagesListParameters parameters,
        CancellationToken cancellationToken = default);

    Task PullImageAsync(
        ImagesCreateParameters parameters,
        AuthConfig authConfig,
        IProgress<JSONMessage> progress,
        CancellationToken cancellationToken = default);
}
```

2. Create a concrete implementation:
```csharp
public class DockerClientWrapper : IDockerClientWrapper
{
    private readonly DockerClient _client;

    public DockerClientWrapper(DockerClient client)
    {
        _client = client;
    }

    // Implement all interface methods by delegating to _client
}
```

3. Update DockerService to use the abstraction:
```csharp
public class DockerService : IDockerService
{
    private readonly IDockerClientWrapper _dockerClient;
    private readonly ILogger<DockerService> _logger;
    // ... rest of the class

    public DockerService(
        IDockerClientWrapper dockerClient,
        AppConfiguration configuration,
        ILogger<DockerService> logger)
    {
        _dockerClient = dockerClient;
        _logger = logger;
        // ... initialization
    }
}
```

4. Update dependency injection:
```csharp
builder.Services.AddSingleton<IDockerClientWrapper, DockerClientWrapper>();
builder.Services.AddSingleton<IDockerService, DockerService>();
```

## Benefits
- Better testability
- Looser coupling
- Easier to swap implementations
- Improved maintainability
