# Issue: Missing Circuit Breaker

## Current Problem
The queue processing doesn't have a circuit breaker pattern.

## Recommendation
Implement a circuit breaker to prevent overwhelming the system during failures.

## Implementation Steps

1. Add Polly for circuit breaker support:
```bash
dotnet add package Polly
```

2. Create a circuit breaker policy:
```csharp
public class QueueProcessor
{
    private readonly ILogger<QueueProcessor> _logger;
    private readonly IDockerService _dockerService;
    private readonly QueueClient _queueClient;
    private readonly AsyncPolicy _circuitBreakerPolicy;

    public QueueProcessor(
        ILogger<QueueProcessor> logger,
        IDockerService dockerService,
        QueueClient queueClient)
    {
        _logger = logger;
        _dockerService = dockerService;
        _queueClient = queueClient;

        _circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (ex, breakDelay) =>
                {
                    _logger.LogWarning(
                        ex, "Circuit breaker opened for {BreakDelay} due to: {ExceptionMessage}",
                        breakDelay, ex.Message);
                },
                onReset: () => _logger.LogInformation("Circuit breaker reset"),
                onHalfOpen: () => _logger.LogInformation("Circuit breaker half-open"));
    }

    public async Task ProcessQueueMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    var message = await _queueClient.ReceiveMessageAsync(cancellationToken);
                    if (message != null)
                    {
                        await ProcessQueueMessageAsync(message, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(DockerServiceConstants.QueueMonitorDelayMs, cancellationToken);
                    }
                });
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing queue message");
                await Task.Delay(DockerServiceConstants.QueueMonitorDelayMs, cancellationToken);
            }
        }
    }
}
```

3. Add fallback policy for better resilience:
```csharp
public QueueProcessor(
    ILogger<QueueProcessor> logger,
    IDockerService dockerService,
    QueueClient queueClient)
{
    _logger = logger;
    _dockerService = dockerService;
    _queueClient = queueClient;

    var fallbackPolicy = Policy.Handle<Exception>().FallbackAsync(
        fallbackAction: async (ct) =>
        {
            _logger.LogWarning("Fallback: Skipping message due to error");
            await Task.CompletedTask;
        },
        onFallbackAsync: async (ex, ct) =>
        {
            _logger.LogError(ex, "Fallback triggered due to error");
            await Task.CompletedTask;
        });

    _circuitBreakerPolicy = Policy.WrapAsync(fallbackPolicy, circuitBreakerPolicy);
}
```

4. Add retry policy for transient failures:
```csharp
public QueueProcessor(
    ILogger<QueueProcessor> logger,
    IDockerService dockerService,
    QueueClient queueClient)
{
    _logger = logger;
    _dockerService = dockerService;
    _queueClient = queueClient;

    var retryPolicy = Policy.Handle<Exception>().RetryAsync(
        retryCount: 3,
        onRetry: (ex, retryCount, context) =>
        {
            _logger.LogWarning(
                ex, "Retry {RetryCount} due to: {ExceptionMessage}",
                retryCount, ex.Message);
        });

    var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
        // ... circuit breaker configuration ...
    );

    _circuitBreakerPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
}
```

5. Add timeout policy to prevent hanging:
```csharp
public QueueProcessor(
    ILogger<QueueProcessor> logger,
    IDockerService dockerService,
    QueueClient queueClient)
{
    _logger = logger;
    _dockerService = dockerService;
    _queueClient = queueClient;

    var timeoutPolicy = Policy.TimeoutAsync(
        timeout: TimeSpan.FromMinutes(1),
        timeoutStrategy: TimeoutStrategy.Pessimistic,
        onTimeoutAsync: async (context, timespan, task, ex) =>
        {
            _logger.LogError("Timeout after {Timeout} seconds", timespan.TotalSeconds);
            await Task.CompletedTask;
        });

    var retryPolicy = Policy.Handle<Exception>().RetryAsync(
        // ... retry configuration ...
    );

    var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
        // ... circuit breaker configuration ...
    );

    _circuitBreakerPolicy = Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
}
```

## Benefits
- Better system resilience
- Prevents system overload during failures
- Improved error handling
- More graceful degradation
- Better monitoring of system health
