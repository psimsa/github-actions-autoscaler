# Issue: Lack of Interface Segregation

## Current Problem
The `IDockerService` interface appears to be large based on the implementation, violating the Interface Segregation Principle.

## Recommendation
Break down the interface into smaller, more specific interfaces.

## Implementation Steps

1. Create separate interfaces for different responsibilities:
```csharp
public interface IContainerManager
{
    Task<IList<ContainerListResponse>> GetAutoscalerContainersAsync();
    Task<bool> StartEphemeralContainer(string repoFullName, string containerName, long jobRunId);
    Task WaitForAvailableRunner();
}

public interface IImageManager
{
    Task<bool> PullImageIfNotExists(CancellationToken token);
}

public interface IWorkflowProcessor
{
    Task<bool> ProcessWorkflow(Workflow? workflow);
}

public interface IRepositoryValidator
{
    bool CheckIfRepoIsWhitelistedOrHasAllowedPrefix(string repositoryFullName);
}
```

2. Update the DockerService to implement these interfaces:
```csharp
public class DockerService : IContainerManager, IImageManager, IWorkflowProcessor, IRepositoryValidator
{
    // Implementation remains the same
}
```

3. Update dependency injection:
```csharp
builder.Services.AddSingleton<IContainerManager, DockerService>();
builder.Services.AddSingleton<IImageManager, DockerService>();
builder.Services.AddSingleton<IWorkflowProcessor, DockerService>();
builder.Services.AddSingleton<IRepositoryValidator, DockerService>();
```

## Benefits
- Better separation of concerns
- Easier to implement mocks for testing
- Clearer interface boundaries
- Improved maintainability
