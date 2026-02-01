using System.Diagnostics;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace GithubActionsAutoscaler.Runner.Docker.Services;

public class ContainerManager : IContainerManager
{
    private readonly DockerClient _client;
    private readonly ILogger<ContainerManager> _logger;
    private readonly IImageManager _imageManager;
    private readonly string _accessToken;
    private readonly string _toolCacheVolumeName;

    private readonly int _maxRunners;
    private readonly string _labelField;
    private readonly Dictionary<string, IDictionary<string, bool>> _autoscalerContainersDefinition =
        new()
        {
            {
                "label",
                new Dictionary<string, bool>() { { "autoscaler", true } }
            },
        };
    private readonly EmptyStruct _emptyStruct = new EmptyStruct();

    public ContainerManager(
        DockerClient client,
        DockerRunnerOptions options,
        IImageManager imageManager,
        ILogger<ContainerManager> logger
    )
    {
        _client = client;
        _logger = logger;
        _imageManager = imageManager;
        _accessToken = options.AccessToken;
        _toolCacheVolumeName = options.ToolCacheVolumeName;
        var maxRunners = options.MaxRunners;
        _maxRunners = maxRunners > 0 ? maxRunners : 3;
        _labelField = string.Join(',', options.Labels).ToLowerInvariant();
    }

    public async Task<IList<ContainerListResponse>> ListContainersAsync()
    {
        return await _client.Containers.ListContainersAsync(
            new ContainersListParameters() { Filters = _autoscalerContainersDefinition, All = true }
        );
    }

    public async Task<bool> CreateAndStartContainerAsync(
        string repositoryFullName,
        string containerName,
        long jobRunId,
        string image
    )
    {
        using var activity = Activity.Current?.Source.StartActivity("ContainerManager.CreateAndStartContainerAsync");
        if ((await ListContainersAsync()).Count >= _maxRunners)
        {
            activity?.AddEvent(new ActivityEvent("Max runners reached, cannot create new container"));
            return false;
        }

        activity?.AddEvent(new ActivityEvent("Creating volumes for new container"));
        var workVolume = await _client.Volumes.CreateAsync(new VolumesCreateParameters());
        var toolCacheVolume = (await _client.Volumes.ListAsync(new VolumesListParameters()
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>()
            {
                {
                    "name",
                    new Dictionary<string, bool>()
                    {
                        { _toolCacheVolumeName, true }
                    }
                }
                }
        })).Volumes.FirstOrDefault() ??
        await _client.Volumes.CreateAsync(new VolumesCreateParameters()
        {
            Name = _toolCacheVolumeName,
        });

        var volumes = new Dictionary<string, EmptyStruct>
        {
            { "/var/run/docker.sock", _emptyStruct },
            { workVolume.Mountpoint, _emptyStruct },
            { toolCacheVolume.Name, _emptyStruct },
        };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(20_000);

        if (!await _imageManager.EnsureImageExistsAsync(image, cts.Token))
            return false;

        var mounts = new List<Mount>(
            [
                new Mount()
                {
                    Target = "/var/run/docker.sock",
                    Source = "/var/run/docker.sock",
                    Type = "bind",
                    ReadOnly = false,
                },
                new Mount()
                {
                    Target = workVolume.Mountpoint,
                    Source = workVolume.Mountpoint,
                    Type = "bind",
                    ReadOnly = false,
                },
                new Mount()
                {
                    Target = toolCacheVolume.Mountpoint,
                    Source = toolCacheVolume.Mountpoint,
                    Type = "bind",
                    ReadOnly = false,
                },
                new Mount()
                {
                    Source = workVolume.Name,
                    Target = "/dummy",
                    ReadOnly = false,
                    Type = "volume",
                },
                new Mount()
                {
                    Source = toolCacheVolume.Name,
                    Target = "/opt/hostedtoolcache",
                    ReadOnly = false,
                    Type = "volume",
                },
            ]
        );

        var container = new CreateContainerParameters()
        {
            Image = image,
            Name = containerName,
            HostConfig = new HostConfig() { AutoRemove = true, Mounts = mounts },
            Volumes = volumes,
            Env =
            [
                "REPO_URL=https://github.com/" + repositoryFullName,
                $"ACCESS_TOKEN={_accessToken}",
                $"RUNNER_WORKDIR={workVolume.Mountpoint}",
                $"RUNNER_NAME={containerName}",
                "EPHEMERAL=TRUE",
                "DISABLE_AUTO_UPDATE=TRUE",
                $"LABELS={_labelField}",
            ],
            Labels = new Dictionary<string, string>()
            {
                { "autoscaler", "true" },
                { "autoscaler.repository", repositoryFullName },
                { "autoscaler.container", containerName },
                { "autoscaler.jobrun", jobRunId.ToString() },
            },
        };

        activity?.AddEvent(new ActivityEvent($"Creating and starting container for {repositoryFullName}"));
        _logger.LogInformation("Creating container for {repositoryFullName}", repositoryFullName);
        var response = await _client.Containers.CreateContainerAsync(container, cts.Token);
        activity?.AddEvent(new ActivityEvent($"Container for {repositoryFullName} created, starting"));
        _logger.LogInformation("Container for {repositoryFullName} created", repositoryFullName);
        int startAttempts = 0;
        while (
            !await _client.Containers.StartContainerAsync(
                response.ID,
                new ContainerStartParameters(),
                cts.Token
            )
        )
        {
            activity?.AddEvent(new ActivityEvent($"Starting container for {repositoryFullName} failed, retrying"));
            startAttempts++;
            if (startAttempts > 5)
            {
                activity?.AddEvent(new ActivityEvent($"Failed to start container for {repositoryFullName} after multiple attempts"));
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "Failed to start container after multiple attempts");
                _logger.LogError(
                    "Failed to start container for {repositoryFullName}",
                    repositoryFullName
                );
                await _client.Containers.RemoveContainerAsync(
                    response.ID,
                    new ContainerRemoveParameters()
                    {
                        Force = true,
                        RemoveLinks = true,
                        RemoveVolumes = true,
                    },
                    cts.Token
                );
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }

        _logger.LogInformation("Container for {repositoryFullName} started", repositoryFullName);
        return true;
    }

    public async Task StartCreatedContainersAsync(CancellationToken token)
    {
        var containers = await ListContainersAsync();
        foreach (
            var createdContainer in containers.Where(_ =>
                _.Status.Equals("created", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            await _client.Containers.StartContainerAsync(
                createdContainer.ID,
                new ContainerStartParameters(),
                token
            );
        }
    }

    public async Task CleanupOldContainersAsync(CancellationToken token)
    {
        bool IsContainerTooOld(ContainerListResponse _) =>
            _.Created.ToUniversalTime().AddHours(1) < DateTime.UtcNow;

        var containers = await ListContainersAsync();
        foreach (var containerListResponse in containers.Where(IsContainerTooOld))
        {
            await _client.Containers.StopContainerAsync(
                containerListResponse.ID,
                new ContainerStopParameters() { WaitBeforeKillSeconds = 20 }
            );
        }
    }

    public async Task PruneVolumesAsync(CancellationToken token)
    {
        await _client.Volumes.PruneAsync(new VolumesPruneParameters(), token);
    }
}
