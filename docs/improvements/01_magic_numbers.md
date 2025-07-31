# Issue: Magic Numbers and Strings

## Current Problem
The code contains several magic numbers and strings that make the code harder to understand and maintain.

## Examples
- `3_000` milliseconds delay in `WaitForAvailableRunner`
- `10_000` milliseconds delay in `QueueMonitorWorker`
- `20_000` milliseconds timeout in `StartEphemeralContainer`
- `1` hour for container age check in `ContainerGuard`

## Recommendation
Define these as named constants at the top of the class or in a configuration file.

## Implementation Steps

1. Create a new static class `DockerServiceConstants`:
```csharp
public static class DockerServiceConstants
{
    public const int RunnerAvailabilityCheckDelayMs = 3_000;
    public const int QueueMonitorDelayMs = 10_000;
    public const int ContainerStartTimeoutMs = 20_000;
    public const int ContainerMaxAgeHours = 1;
    public const string DefaultDockerImage = "myoung34/github-runner:latest";
}
```

2. Replace magic numbers with constants:
```csharp
public async Task WaitForAvailableRunner()
{
    while ((await GetAutoscalerContainersAsync()).Count >= _maxRunners)
        await Task.Delay(DockerServiceConstants.RunnerAvailabilityCheckDelayMs);
}
```

3. For the container age check:
```csharp
bool IsContainerTooOld(ContainerListResponse _) =>
    _.Created.ToUniversalTime().AddHours(DockerServiceConstants.ContainerMaxAgeHours) < DateTime.UtcNow;
```

## Benefits
- Improved code readability
- Easier maintenance when values need to change
- Centralized configuration
- Better understanding of what values represent
