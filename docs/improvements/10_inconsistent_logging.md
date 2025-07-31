# Issue: Inconsistent Logging

## Current Problem
Logging is inconsistent throughout the codebase.

## Recommendation
Define a consistent logging strategy and apply it throughout the codebase.

## Implementation Steps

1. Create a logging helper class:
```csharp
public static class LoggerExtensions
{
    public static void LogInformation(this ILogger logger, string message, params object[] args)
    {
        logger.LogInformation(message, args);
    }

    public static void LogWarning(this ILogger logger, string message, params object[] args)
    {
        logger.LogWarning(message, args);
    }

    public static void LogError(this ILogger logger, Exception ex, string message, params object[] args)
    {
        logger.LogError(ex, message, args);
    }

    public static void LogDebug(this ILogger logger, string message, params object[] args)
    {
        logger.LogDebug(message, args);
    }
}
```

2. Define logging levels for different scenarios:
```csharp
public static class LogLevelConstants
{
    public const string Info = "Information";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string Debug = "Debug";
}
```

3. Update all logging calls to use consistent format:
```csharp
_logger.LogInformation(
    "Starting container for repository {RepositoryName} with job {JobName}",
    repositoryFullName,
    workflow.Job.Name
);

_logger.LogError(
    ex,
    "Failed to start container for repository {RepositoryName} with job {JobName}",
    repositoryFullName,
    workflow.Job.Name
);

_logger.LogDebug(
    "Container {ContainerId} has status {ContainerStatus}",
    container.ID,
    container.Status
);
```

4. Add structured logging for important events:
```csharp
_logger.LogInformation(
    "Processing workflow event {EventType} for repository {RepositoryName} and job {JobName}",
    workflow.Action,
    workflow.Repository.FullName,
    workflow.Job.Name
);

_logger.LogInformation(
    "Container {ContainerId} started successfully",
    container.ID
);
```

5. Add logging for critical operations:
```csharp
_logger.LogInformation(
    "Checking for available runners. Current count: {CurrentCount}, Max allowed: {MaxCount}",
    currentCount,
    _maxRunners
);

_logger.LogInformation(
    "Pulling Docker image {ImageName} with tag {ImageTag}",
    imageName,
    imageTag
);
```

## Benefits
- Consistent logging format
- Better visibility into system operations
- Easier debugging and troubleshooting
- Improved monitoring capabilities
- Standardized log messages
