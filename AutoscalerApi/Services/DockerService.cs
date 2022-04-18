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

    public DockerService(DockerClient client, IConfiguration configuration, ILogger<DockerService> logger)
    {
        _client = client;
        _logger = logger;
        _accessToken = configuration["ACCESS_TOKEN"];
    }

    public async Task StartEphemeralContainer(string repositoryFullName)
    {
        var volumes = new Dictionary<string, EmptyStruct> { { "/var/run/docker.sock", new EmptyStruct() } };

        var mounts = new List<Mount>(new[]
        {
            new Mount()
            {
                Target = "/var/run/docker.sock", Source = "/var/run/docker.sock", Type = "bind",
                ReadOnly = false
            }
        });

        var container = new CreateContainerParameters()
        {
            Image = "myoung34/github-runner",
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
                "RUNNER_WORKDIR=/runner",
                "EPHEMERAL=TRUE",
            })
        };

        _logger.LogInformation($"Creating container for {repositoryFullName}");
        var response = await _client.Containers.CreateContainerAsync(container);
        _logger.LogInformation($"Container for {repositoryFullName} created");
        await _client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
        _logger.LogInformation($"Container for {repositoryFullName} started");
    }

    public async Task ProcessWorkflow(Workflow workflow)
    {
        if (workflow.action == "queued" &&
            workflow.repository.FullName.StartsWith("ofcoursedude/") &&
            workflow.job.labels.Any(_ => _ == "self-hosted"))
        {
            _logger.LogInformation($"Workflow is self-hosted");
            await StartEphemeralContainer(workflow.repository.FullName);
        }
    }
}

public interface IDockerService
{
    Task StartEphemeralContainer(string repositoryFullName);
    Task ProcessWorkflow(Workflow workflow);
}
