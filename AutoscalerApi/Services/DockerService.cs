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

    public DockerService(DockerClient client, IConfiguration configuration, ILogger<DockerService> logger)
    {
        _client = client;
        _logger = logger;
        _accessToken = configuration["ACCESS_TOKEN"];
        _dockerToken = configuration["DOCKER_TOKEN"];
    }

    private async Task StartEphemeralContainer(string repositoryFullName, string containerName)
    {
        var volume = await _client.Volumes.CreateAsync(new VolumesCreateParameters());
        // var r2 = await _client.Volumes.PruneAsync();

        var volumes = new Dictionary<string, EmptyStruct> { { "/var/run/docker.sock", new EmptyStruct() }, {volume.Name, new EmptyStruct()} };

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
                Source = volume.Name, Target = "/home/runner/work", ReadOnly = false
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
                // CapAdd = new List<string>() { "SETFCAP", "FOWNER", "SYS_CHROOT" },
            },
            Volumes = volumes,
            Env = new List<string>(new[]
            {
                "REPO_URL=https://github.com/" + repositoryFullName,
                $"ACCESS_TOKEN={_accessToken}",
                "RUNNER_WORKDIR=/home/runner/work",
                "EPHEMERAL=TRUE",
            })
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
            }, null, new Progress<JSONMessage>(message =>
            {
                if (message.Status.StartsWith("Status:"))
                {
                    m.Set();
                }
            }));
        // m.Wait();
        _logger.LogInformation("Downloaded");
    }

    public async Task ProcessWorkflow(Workflow workflow)
    {
        switch (workflow.Action)
        {
            case "queued" when workflow.Repository.FullName.StartsWith("ofcoursedude/") && workflow.Job.Labels.Any(_ => _ == "self-hosted"):
                _logger.LogInformation($"Workflow is self-hosted");
                await StartEphemeralContainer(workflow.Repository.FullName,
                    $"{workflow.Repository.Name}-{workflow.Job.RunId}");
                ;
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
