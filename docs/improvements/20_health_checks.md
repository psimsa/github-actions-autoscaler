# Issue: Missing Health Checks

## Current Problem
There are no health check endpoints.

## Recommendation
Add health check endpoints to monitor the service status.

## Implementation Steps

1. Add health check packages:
```bash
dotnet add package AspNetCore.HealthChecks
```

2. Add health check services:
```csharp
public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DockerHealthCheck>("docker")
            .AddCheck<QueueHealthCheck>("queue")
            .AddCheck<MemoryHealthCheck>("memory", failureStatus: HealthStatus.Degraded)
            .AddCheck<StartupHealthCheck>("startup");

        return services;
    }
}
```

3. Create health check implementations:
```csharp
public class DockerHealthCheck : IHealthCheck
{
    private readonly IDockerClientWrapper _dockerClient;

    public DockerHealthCheck(IDockerClientWrapper dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containers = await _dockerClient.ListContainersAsync(
                new ContainersListParameters { All = true }, cancellationToken);
            return HealthCheckResult.Healthy("Docker client is working");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Docker client failed", ex);
        }
    }
}

public class QueueHealthCheck : IHealthCheck
{
    private readonly QueueClient _queueClient;

    public QueueHealthCheck(QueueClient queueClient)
    {
        _queueClient = queueClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var properties = await _queueClient.GetPropertiesAsync(cancellationToken);
            return HealthCheckResult.Healthy("Queue is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Queue is not accessible", ex);
        }
    }
}
```

4. Add health check endpoint:
```csharp
public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        return endpoints;
    }
}
```

5. Update Program.cs:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomHealthChecks();

var app = builder.Build();

app.MapHealthEndpoints();

app.Run();
```

6. Add readiness and liveness probes:
```csharp
public class StartupHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Service is starting up"));
    }
}
```

## Benefits
- Better system monitoring
- Improved observability
- Easier troubleshooting
- Health status reporting
- Integration with orchestrators
