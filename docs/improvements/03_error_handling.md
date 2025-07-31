# Issue: Missing Error Handling

## Current Problem
Some operations lack proper error handling, which could lead to silent failures.

## Examples
- In `PullImageIfNotExists`, there's no handling if `CreateImageAsync` fails
- In `StartEphemeralContainer`, container creation failures are only logged after 5 attempts

## Recommendation
Add proper error handling with appropriate logging and fallback mechanisms.

## Implementation Steps

1. Improve error handling in `PullImageIfNotExists`:
```csharp
private async Task<bool> PullImageIfNotExists(CancellationToken token)
{
    try
    {
        // Existing code...
        var t = Task.Run(async () =>
        {
            try
            {
                await _client.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = imageFields[0],
                        Tag = imageFields.Length == 2 ? imageFields[1] : "latest",
                    },
                    new AuthConfig() { Password = _dockerToken },
                    new Progress<JSONMessage>(message =>
                    {
                        if (message.Status.StartsWith("Status:"))
                        {
                            m.Set();
                        }
                    }),
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pull image {Image}", _dockerImage);
                throw;
            }
        }, token);

        // Rest of the method...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error checking/pulling image");
        return false;
    }
}
```

2. Improve container creation error handling:
```csharp
private async Task<bool> StartEphemeralContainer(
    string repositoryFullName,
    string containerName,
    long jobRunId)
{
    try
    {
        // Existing code...
        var response = await _client.Containers.CreateContainerAsync(container, cts.Token);
        
        int startAttempts = 0;
        const int maxStartAttempts = 5;
        while (!await _client.Containers.StartContainerAsync(
            response.ID,
            new ContainerStartParameters(),
            cts.Token))
        {
            startAttempts++;
            if (startAttempts > maxStartAttempts)
            {
                _logger.LogError(
                    "Failed to start container for {Repository} after {Attempts} attempts",
                    repositoryFullName, maxStartAttempts);
                await CleanupFailedContainer(response.ID);
                return false;
            }
            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }
        // Rest of the method...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create/start container for {Repository}", repositoryFullName);
        return false;
    }
}

private async Task CleanupFailedContainer(string containerId)
{
    try
    {
        await _client.Containers.RemoveContainerAsync(
            containerId,
            new ContainerRemoveParameters()
            {
                Force = true,
                RemoveLinks = true,
                RemoveVolumes = true,
            },
            CancellationToken.None
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to cleanup container {ContainerId}", containerId);
    }
}
```

## Benefits
- Better error visibility
- Improved system stability
- Easier debugging
- More graceful failure handling
