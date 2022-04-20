using System.Collections.Generic;
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
    private DateTime _lastPullCheck = DateTime.MinValue;

    public DockerService(DockerClient client, IConfiguration configuration, ILogger<DockerService> logger)
    {
        _client = client;
        _logger = logger;
        _accessToken = configuration["ACCESS_TOKEN"];
        _dockerToken = configuration["DOCKER_TOKEN"];
        var maxRunners = configuration.GetValue<int>("MAX_RUNNERS");
        _maxRunners = maxRunners > 0 ? maxRunners : 3;
    }

    private async Task StartEphemeralContainer(string repositoryFullName, string containerName, long jobRunId)
    {
        async Task<IList<ContainerListResponse>> ListContainersAsync()
        {
            return await _client.Containers.ListContainersAsync(new ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>()
                {
                    {
                        "label", new Dictionary<string, bool>()
                        {
                            { "autoscaler=true", true }
                        }
                    }
                }
            });
        }

        while ((await ListContainersAsync()).Count == _maxRunners)
        {
            await Task.Delay(1000);
        }

        var volume = await _client.Volumes.CreateAsync(new VolumesCreateParameters());

        var volumes = new Dictionary<string, EmptyStruct>
        {
            { "/var/run/docker.sock", new EmptyStruct() },
            { volume.Mountpoint, new EmptyStruct() }
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
                "EPHEMERAL=TRUE",
                "DISABLE_AUTO_UPDATE=TRUE",
            }),
            Labels = new Dictionary<string, string>()
            {
                { "autoscaler", "true" },
                { "autoscaler.repository", repositoryFullName },
                { "autoscaler.container", containerName },
                { "autoscaler.jobrun", jobRunId.ToString() }
            }
        };

        _logger.LogInformation($"Creating container for {repositoryFullName}");
        var response = await _client.Containers.CreateContainerAsync(container);
        _logger.LogInformation($"Container for {repositoryFullName} created");
        await _client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
        _logger.LogInformation($"Container for {repositoryFullName} started");
    }

    private async Task PullImageIfNotExists()
    {
        var m = new ManualResetEventSlim();
        var progress = new Progress<JSONMessage>();
        await _client.Images.CreateImageAsync(
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
                }));
        m.Wait();
        _logger.LogInformation("Downloaded");
    }

    public async Task ProcessWorkflow(Workflow workflow)
    {
        switch (workflow.Action)
        {
            case "queued" when workflow.Repository.FullName.StartsWith("ofcoursedude/") &&
                               workflow.Job.Labels.Any(_ => _ == "self-hosted"):
                _logger.LogInformation($"Workflow is self-hosted");
                await StartEphemeralContainer(workflow.Repository.FullName,
                    $"{workflow.Repository.Name}-{workflow.Job.RunId}", workflow.Job.RunId);
                break;
            case "completed":
                await _client.Volumes.PruneAsync();
                break;
        }
    }
}

public interface IDockerService
{
    Task ProcessWorkflow(Workflow workflow);
}
