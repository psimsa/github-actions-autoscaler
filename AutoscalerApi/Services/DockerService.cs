using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoscalerApi.Controllers;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace AutoscalerApi.Services;

public class DockerService : IDockerService
{
    private readonly DockerClient _client;
    private readonly ILogger<DockerService> _logger;
    private readonly string _accessToken;
    private readonly string _dockerToken;
    private readonly int _maxRunners;
    private readonly string _repoPrefix;
    private readonly string[] _repoWhitelist;
    private readonly bool _isRepoWhitelistExactMatch;
    private DateTime _lastPullCheck = DateTime.MinValue;
    private int _totalCount = 0;

    public DockerService(DockerClient client, AppConfiguration configuration, ILogger<DockerService> logger)
    {
        _client = client;
        _logger = logger;
        _accessToken = configuration.GithubToken;
        _dockerToken = configuration.DockerToken;
        var maxRunners = configuration.MaxRunners;
        _maxRunners = maxRunners > 0 ? maxRunners : 3;
        _repoPrefix = configuration.RepoWhitelistPrefix;
        _repoWhitelist = configuration.RepoWhitelist;
        _isRepoWhitelistExactMatch = configuration.IsRepoWhitelistExactMatch;
    }

    public async Task<IList<ContainerListResponse>> GetAutoscalerContainersAsync()
    {
        return await _client.Containers.ListContainersAsync(new ContainersListParameters()
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>()
            {
                {
                    "label", new Dictionary<string, bool>()
                    {
                        {"autoscaler=true", true}
                    }
                }
            }
        });
    }

    private async Task StartEphemeralContainer(string repositoryFullName, string containerName, long jobRunId)
    {
        while ((await GetAutoscalerContainersAsync()).Count >= _maxRunners)
        {
            await Task.Delay(3000);
        }

        var volume = await _client.Volumes.CreateAsync(new VolumesCreateParameters());

        var volumes = new Dictionary<string, EmptyStruct>
        {
            {"/var/run/docker.sock", new EmptyStruct()},
            {volume.Mountpoint, new EmptyStruct()}
        };

        await PullImageIfNotExists();

        var mounts = new List<Mount>(new[]
        {
            new Mount()
            {
                Target = "/var/run/docker.sock", Source = "/var/run/docker.sock", Type = "bind",
                ReadOnly = false
            },
            new Mount()
            {
                Target = volume.Mountpoint, Source = volume.Mountpoint, Type = "bind",
                ReadOnly = false
            },
            new Mount()
            {
                Source = volume.Name, Target = "/dummy", ReadOnly = false, Type = "volume"
            }
        });

        var container = new CreateContainerParameters()
        {
            Image = "myoung34/github-runner",
            Name = containerName,
            HostConfig = new HostConfig()
            {
                AutoRemove = true,
                Mounts = mounts,
            },
            Volumes = volumes,
            Env = new List<string>(new[]
            {
                "REPO_URL=https://github.com/" + repositoryFullName,
                $"ACCESS_TOKEN={_accessToken}",
                $"RUNNER_WORKDIR={volume.Mountpoint}",
                $"RUNNER_NAME={containerName}",
                "EPHEMERAL=TRUE",
                "DISABLE_AUTO_UPDATE=TRUE",
            }),
            Labels = new Dictionary<string, string>()
            {
                {"autoscaler", "true"},
                {"autoscaler.repository", repositoryFullName},
                {"autoscaler.container", containerName},
                {"autoscaler.jobrun", jobRunId.ToString()}
            }
        };

        _logger.LogInformation("Creating container for {repositoryFullName}", repositoryFullName);
        var response = await _client.Containers.CreateContainerAsync(container);
        _logger.LogInformation("Container for {repositoryFullName} created", repositoryFullName);
        await _client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
        _logger.LogInformation("Container for {repositoryFullName} started", repositoryFullName);
    }

    private async Task PullImageIfNotExists()
    {
        var imagesListResponses = await _client.Images.ListImagesAsync(new ImagesListParameters() {All = true});
        var tags = imagesListResponses
            .Where(_ => _.RepoTags != null && _.RepoTags.Count > 0).SelectMany(_ => _.RepoTags);

        if (tags.Any(_ => _.Equals("myoung34/github-runner:latest")) &&
            _lastPullCheck.AddHours(1) > DateTime.UtcNow)
        {
            return;
        }

        _logger.LogInformation("Checking for latest image");

        _lastPullCheck = DateTime.UtcNow;
        var m = new ManualResetEventSlim();
        var progress = new Progress<JSONMessage>();
        await _client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "myoung34/github-runner",
                Tag = "latest",
            }, new AuthConfig() {Password = _dockerToken}, new Progress<JSONMessage>(
                message =>
                {
                    if (message.Status.StartsWith("Status:"))
                    {
                        m.Set();
                    }
                }));
        m.Wait();
        _logger.LogInformation("Downloaded new docker image");
    }

    public async Task ProcessWorkflow(Workflow workflow)
    {
        switch (workflow.Action)
        {
            case "queued" when CheckIfRepoIsWhitelistedOrHasAllowedPrefix(workflow.Repository.FullName) &&
                               workflow.Job.Labels.Any(_ => _ == "self-hosted"):
                _logger.LogInformation(
                    "Workflow '{workflow}' is self-hosted and repository {repository} whitelisted, starting container",
                    workflow.Job.Name, workflow.Repository.FullName);
                Interlocked.Increment(ref _totalCount);
                var containerName = $"{workflow.Repository.Name}-{workflow.Job.RunId}-{_totalCount}";
                await StartEphemeralContainer(workflow.Repository.FullName,
                    containerName, workflow.Job.RunId);
                break;
            case "completed":
                await _client.Volumes.PruneAsync();
                break;
        }
    }

    private bool CheckIfRepoIsWhitelistedOrHasAllowedPrefix(string repositoryFullName)
    {
        if (repositoryFullName.StartsWith(_repoPrefix))
        {
            return true;
        }

        if (_repoWhitelist.Length == 0)
        {
            return false;
        }

        if (_isRepoWhitelistExactMatch)
        {
            return _repoWhitelist.Contains(repositoryFullName);
        }

        return _repoWhitelist.Any(_ => repositoryFullName.StartsWith(_) || _.Equals("*"));
    }
}

public interface IDockerService
{
    Task ProcessWorkflow(Workflow workflow);
    Task<IList<ContainerListResponse>> GetAutoscalerContainersAsync();
}