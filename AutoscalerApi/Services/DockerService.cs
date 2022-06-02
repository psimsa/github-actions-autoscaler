using AutoscalerApi.Models;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace AutoscalerApi.Services;

public class DockerService : IDockerService
{
    private readonly DockerClient _client;
    private readonly ILogger<DockerService> _logger;
    private readonly string _accessToken;
    private readonly string _dockerToken;
    private readonly int _maxRunners;
    private readonly string _repoWhitelistPrefix;
    private readonly string[] _repoWhitelist;
    private readonly bool _isRepoWhitelistExactMatch;
    private DateTime _lastPullCheck = DateTime.MinValue;
    private int _totalCount = 0;
    private readonly string _repoBlacklistPrefix;
    private readonly string[] _repoBlacklist;
    private readonly bool _isRepoBlacklistExactMatch;
    private readonly string[] _labels;
    private readonly string _labelField;

    private Task _containerGuardTask;

    private readonly Dictionary<string, IDictionary<string, bool>> _autoscalerContainersDefinition = new()
    {
        {
            "label", new Dictionary<string, bool>()
            {
                { "autoscaler", true }
            }
        }
    };

    private EmptyStruct _emptyStruct = new EmptyStruct();

    public DockerService(DockerClient client, AppConfiguration configuration, ILogger<DockerService> logger)
    {
        _client = client;
        _logger = logger;
        _accessToken = configuration.GithubToken;
        _dockerToken = configuration.DockerToken;
        var maxRunners = configuration.MaxRunners;
        _maxRunners = maxRunners > 0 ? maxRunners : 3;
        _repoWhitelistPrefix = configuration.RepoWhitelistPrefix;
        _repoWhitelist = configuration.RepoWhitelist;
        _isRepoWhitelistExactMatch = configuration.IsRepoWhitelistExactMatch;
        _repoBlacklistPrefix = configuration.RepoBlacklistPrefix;
        _repoBlacklist = configuration.RepoBlacklist;
        _isRepoBlacklistExactMatch = configuration.IsRepoBlacklistExactMatch;
        _labels = configuration.Labels;
        _labelField = string.Join(',', _labels).ToLowerInvariant();
        _containerGuardTask = ContainerGuard(CancellationToken.None);
    }

    public async Task<IList<ContainerListResponse>> GetAutoscalerContainersAsync()
    {
        return await _client.Containers.ListContainersAsync(new ContainersListParameters()
        {
           Filters = _autoscalerContainersDefinition,
           All = true
        });
    }

    public async Task<bool> ProcessWorkflow(Workflow? workflow)
    {
        switch (workflow?.Action)
        {
            case "queued" when workflow.Job.Labels.All(l => l != "self-hosted"):
                _logger.LogInformation("Removing non-selfhosted job {jobName} from queue", workflow.Job.Name);
                return true;
            case "queued" when !CheckIfHasAllLabels(workflow.Job.Labels):
                _logger.LogInformation("Job {jobName} does not have all necessary labels, returning to queue", workflow.Job.Name);
                return false;
            case "queued" when CheckIfRepoIsWhitelistedOrHasAllowedPrefix(workflow.Repository.FullName):
                _logger.LogInformation(
                    "Workflow '{Workflow}' is self-hosted and repository {Repository} whitelisted, starting container",
                    workflow.Job.Name, workflow.Repository.FullName);
                Interlocked.Increment(ref _totalCount);
                var containerName = $"{workflow.Repository.Name}-{workflow.Job.RunId}-{_totalCount}";
                return await StartEphemeralContainer(workflow.Repository.FullName,
                    containerName, workflow.Job.RunId);
            case "completed":
                await _client.Volumes.PruneAsync();
                break;
            case null:
                break;
        }

        return true;
    }

    public async Task WaitForAvailableRunner()
    {
        while ((await GetAutoscalerContainersAsync()).Count >= _maxRunners) await Task.Delay(3_000);
    }

    private async Task ContainerGuard(CancellationToken token)
    {
        bool IsContainerTooOld(ContainerListResponse _) => _.Created.ToUniversalTime().AddHours(1) < DateTime.UtcNow;

        while (!token.IsCancellationRequested)
        {
            var containers = await GetAutoscalerContainersAsync();
            if (!containers.Any())
                return;

            foreach (var createdContainer in containers.Where(_ =>
                         _.Status.Equals("created", StringComparison.OrdinalIgnoreCase)))
            {
                await _client.Containers.StartContainerAsync(createdContainer.ID, new ContainerStartParameters(),
                    token);
            }


            foreach (var containerListResponse in containers.Where(IsContainerTooOld))
            {
                await _client.Containers.StopContainerAsync(containerListResponse.ID,
                    new ContainerStopParameters() { WaitBeforeKillSeconds = 20 });
            }

            await Task.Delay(TimeSpan.FromMinutes(2), token);
        }
    }

    private bool CheckIfHasAllLabels(string[] jobLabels)
    {
        return jobLabels.All(l => _labels.Contains(l.ToLowerInvariant())) && jobLabels.Any(l => l == "self-hosted");
    }

    private async Task<bool> StartEphemeralContainer(string repositoryFullName, string containerName, long jobRunId)
    {
        if ((await GetAutoscalerContainersAsync()).Count >= _maxRunners)
        {
            return false;
        }

        var volume = await _client.Volumes.CreateAsync(new VolumesCreateParameters());

        var volumes = new Dictionary<string, EmptyStruct>
        {
            { "/var/run/docker.sock", _emptyStruct },
            { volume.Mountpoint, _emptyStruct }
        };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(20_000);

        if (!await PullImageIfNotExists(cts.Token))
            return false;

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
                $"LABELS={_labelField}",
            }),
            Labels = new Dictionary<string, string>()
            {
                { "autoscaler", "true" },
                { "autoscaler.repository", repositoryFullName },
                { "autoscaler.container", containerName },
                { "autoscaler.jobrun", jobRunId.ToString() }
            }
        };

        _logger.LogInformation("Creating container for {repositoryFullName}", repositoryFullName);
        var response = await _client.Containers.CreateContainerAsync(container, cts.Token);
        _logger.LogInformation("Container for {repositoryFullName} created", repositoryFullName);
        int startAttempts = 0;
        while (!await _client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(), cts.Token))
        {
            startAttempts++;
            if (startAttempts > 5)
            {
                _logger.LogError("Failed to start container for {repositoryFullName}", repositoryFullName);
                await _client.Containers.RemoveContainerAsync(response.ID, new ContainerRemoveParameters()
                {
                    Force = true, RemoveLinks = true, RemoveVolumes = true
                }, cts.Token);
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }

        _logger.LogInformation("Container for {repositoryFullName} started", repositoryFullName);
        if (_containerGuardTask.IsCompleted)
        {
            _containerGuardTask = ContainerGuard(CancellationToken.None);
        }

        return true;
    }

    private async Task<bool> PullImageIfNotExists(CancellationToken token)
    {
        var success = true;

        var imagesListResponses =
            await _client.Images.ListImagesAsync(new ImagesListParameters() { All = true }, token);
        var tags = imagesListResponses
            .Where(_ => _.RepoTags is { Count: > 0 }).SelectMany(_ => _.RepoTags);

        if (tags.Any(_ => _.Equals("myoung34/github-runner:latest")) &&
            _lastPullCheck.AddHours(1) > DateTime.UtcNow)
        {
            return success;
        }

        _logger.LogInformation("Checking for latest image");

        _lastPullCheck = DateTime.UtcNow;
        var m = new ManualResetEventSlim();

        var progress = new Progress<JSONMessage>();
        var t = Task.Run(async () => await _client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "myoung34/github-runner",
                Tag = "latest",
            }, new AuthConfig() { Password = _dockerToken }, new Progress<JSONMessage>(
                message =>
                {
                    if (message.Status.StartsWith("Status:"))
                    {
                        m.Set();
                    }
                }), CancellationToken.None), token);

        WaitHandle.WaitAny(new[] { m.WaitHandle, token.WaitHandle });

        if (token.IsCancellationRequested)
            return false;

        _logger.LogInformation("Downloaded new docker image");
        return success;
    }

    private bool CheckIfRepoIsWhitelistedOrHasAllowedPrefix(string repositoryFullName)
    {
        bool IsRepoBlacklisted(string repoName)
        {
            return repoName switch
            {
                var f when string.IsNullOrWhiteSpace(_repoBlacklistPrefix) && !_repoBlacklist.Any() => false,
                var f when f.StartsWith(_repoBlacklistPrefix) => true,
                var f when _isRepoBlacklistExactMatch => _repoBlacklist.Contains(f),
                _ => _repoBlacklist.Any(repoName.StartsWith)
            };
        }

        return repositoryFullName switch
        {
            var f when IsRepoBlacklisted(f) => false,
            var f when f.StartsWith(_repoWhitelistPrefix) => true,
            _ when _repoWhitelist.Length == 0 => false,
            var f when _isRepoWhitelistExactMatch => _repoWhitelist.Contains(f),
            _ => _repoWhitelist.Any(repo => repositoryFullName.StartsWith(repo) || repo.Equals("*"))
        };
    }
}
