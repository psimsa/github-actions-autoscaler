# Issue: Complex Methods

## Current Problem
Some methods are too complex and do too much.

## Examples
- `StartEphemeralContainer` handles container creation, volume setup, and starting
- `ProcessWorkflow` handles multiple workflow states

## Recommendation
Break down complex methods into smaller, single-responsibility methods.

## Implementation Steps

1. Break down `StartEphemeralContainer`:
```csharp
private async Task<bool> StartEphemeralContainer(
    string repositoryFullName,
    string containerName,
    long jobRunId)
{
    if (await GetAutoscalerContainersAsync().Count >= _maxRunners)
    {
        return false;
    }

    var volume = await CreateVolumeAsync();
    if (volume == null)
    {
        return false;
    }

    if (!await PullImageIfNotExists(CancellationToken.None))
    {
        return false;
    }

    var container = CreateContainerParameters(repositoryFullName, containerName, volume, jobRunId);
    if (container == null)
    {
        return false;
    }

    return await StartContainerAsync(container, volume.Name);
}

private async Task<VolumesCreateResponse> CreateVolumeAsync()
{
    try
    {
        return await _client.Volumes.CreateAsync(new VolumesCreateParameters());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create volume");
        return null;
    }
}

private CreateContainerParameters CreateContainerParameters(
    string repositoryFullName,
    string containerName,
    VolumesCreateResponse volume,
    long jobRunId)
{
    try
    {
        var mounts = new List<Mount>
        {
            new Mount
            {
                Target = "/var/run/docker.sock",
                Source = "/var/run/docker.sock",
                Type = "bind",
                ReadOnly = false,
            },
            new Mount
            {
                Target = volume.Mountpoint,
                Source = volume.Mountpoint,
                Type = "bind",
                ReadOnly = false,
            },
            new Mount
            {
                Source = volume.Name,
                Target = "/dummy",
                ReadOnly = false,
                Type = "volume",
            },
        };

        return new CreateContainerParameters
        {
            Image = _dockerImage,
            Name = containerName,
            HostConfig = new HostConfig { AutoRemove = true, Mounts = mounts },
            Volumes = new Dictionary<string, EmptyStruct>
            {
                { "/var/run/docker.sock", _emptyStructValue },
                { volume.Mountpoint, _emptyStructValue },
            },
            Env = new List<string>
            {
                "REPO_URL=https://github.com/" + repositoryFullName,
                $"ACCESS_TOKEN={_accessToken}",
                $"RUNNER_WORKDIR={volume.Mountpoint}",
                $"RUNNER_NAME={containerName}",
                "EPHEMERAL=TRUE",
                "DISABLE_AUTO_UPDATE=TRUE",
                $"LABELS={_labelField}",
            },
            Labels = new Dictionary<string, string>
            {
                { "autoscaler", "true" },
                { "autoscaler.repository", repositoryFullName },
                { "autoscaler.container", containerName },
                { "autoscaler.jobrun", jobRunId.ToString() },
            },
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create container parameters");
        return null;
    }
}

private async Task<bool> StartContainerAsync(CreateContainerParameters container, string volumeName)
{
    try
    {
        _logger.LogInformation("Creating container for {repositoryFullName}", container.Name);
        var response = await _client.Containers.CreateContainerAsync(container, CancellationToken.None);
        _logger.LogInformation("Container for {repositoryFullName} created", container.Name);

        int startAttempts = 0;
        const int maxStartAttempts = 5;
        while (!await _client.Containers.StartContainerAsync(
            response.ID,
            new ContainerStartParameters(),
            CancellationToken.None))
        {
            startAttempts++;
            if (startAttempts > maxStartAttempts)
            {
                _logger.LogError(
                    "Failed to start container for {repositoryFullName} after {attempts} attempts",
                    container.Name, maxStartAttempts);
                await CleanupFailedContainer(response.ID, volumeName);
                return false;
            }
            await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        _logger.LogInformation("Container for {repositoryFullName} started", container.Name);
        if (_containerGuardTask.IsCompleted)
        {
            _containerGuardTask = ContainerGuard(CancellationToken.None);
        }

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to start container");
        return false;
    }
}

private async Task CleanupFailedContainer(string containerId, string volumeName)
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
        await _client.Volumes.RemoveAsync(volumeName, CancellationToken.None);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to cleanup container {containerId} and volume {volumeName}", containerId, volumeName);
    }
}
```

2. Break down `ProcessWorkflow`:
```csharp
public async Task<bool> ProcessWorkflow(Workflow? workflow)
{
    if (workflow == null)
    {
        return true;
    }

    return workflow.Action switch
    {
        "queued" => await HandleQueuedWorkflow(workflow),
        "completed" => await HandleCompletedWorkflow(workflow),
        _ => true
    };
}

private async Task<bool> HandleQueuedWorkflow(Workflow workflow)
{
    if (workflow.Job.Labels.All(l => l != "self-hosted"))
    {
        _logger.LogInformation(
            "Removing non-selfhosted job {jobName} from queue",
            workflow.Job.Name
        );
        return true;
    }

    if (!CheckIfHasAllLabels(workflow.Job.Labels))
    {
        _logger.LogInformation(
            "Job {jobName} does not have all necessary labels, returning to queue",
            workflow.Job.Name
        );
        return false;
    }

    if (CheckIfRepoIsWhitelistedOrHasAllowedPrefix(workflow.Repository.FullName))
    {
        _logger.LogInformation(
            "Workflow '{Workflow}' is self-hosted and repository {Repository} whitelisted, starting container",
            workflow.Job.Name,
            workflow.Repository.FullName
        );
        Interlocked.Increment(ref _totalCount);
        var containerName = 
            $"{Environment.MachineName}-{workflow.Repository.Name}-{workflow.Job.RunId}-{_totalCount}";
        return await StartEphemeralContainer(
            workflow.Repository.FullName,
            containerName,
            workflow.Job.RunId
        );
    }

    return false;
}

private async Task<bool> HandleCompletedWorkflow(Workflow workflow)
{
    await _client.Volumes.PruneAsync();
    return true;
}
```

## Benefits
- Improved code readability
- Easier testing and debugging
- Better separation of concerns
- More maintainable code
