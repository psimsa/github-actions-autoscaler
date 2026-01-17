using Docker.DotNet.Models;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Models;
using GithubActionsAutoscaler.Services;
using Microsoft.Extensions.Logging;

namespace GithubActionsAutoscaler.Tests.Unit.Services;

public class DockerServiceTests
{
    private readonly Mock<IContainerManager> _containerManagerMock;
    private readonly Mock<IRepositoryFilter> _repositoryFilterMock;
    private readonly Mock<ILabelMatcher> _labelMatcherMock;
    private readonly Mock<ILogger<DockerService>> _loggerMock;
    private readonly AppConfiguration _config;
    private readonly DockerService _service;

    public DockerServiceTests()
    {
        _containerManagerMock = new Mock<IContainerManager>();
        _repositoryFilterMock = new Mock<IRepositoryFilter>();
        _labelMatcherMock = new Mock<ILabelMatcher>();
        _loggerMock = new Mock<ILogger<DockerService>>();
        _config = new AppConfiguration { MaxRunners = 2, DockerImage = "test-image" };

        _service = new DockerService(
            _containerManagerMock.Object,
            _config,
            _repositoryFilterMock.Object,
            _labelMatcherMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ProcessWorkflowAsync_WhenLabelsDoNotMatch_ReturnsFalse()
    {
        // To hit the label check, we must first pass the "non-selfhosted" check
        // So we must include "self-hosted" label
        var workflow = new Workflow(
            "queued",
            new WorkflowJob("job1", ["self-hosted", "wrong"], 1),
            new Repository("repo", "name")
        );
        _labelMatcherMock.Setup(x => x.HasAllRequiredLabels(It.IsAny<string[]>())).Returns(false);

        var result = await _service.ProcessWorkflowAsync(workflow);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessWorkflowAsync_WhenNonSelfHostedJob_ReturnsTrueAndLogs()
    {
        // Logic: case "queued" when workflow.Job.Labels.All(l => l != "self-hosted") -> return true;
        var workflow = new Workflow(
            "queued",
            new WorkflowJob("job1", ["other"], 1),
            new Repository("repo", "name")
        );

        var result = await _service.ProcessWorkflowAsync(workflow);

        result.Should().BeTrue();
        _containerManagerMock.Verify(
            x =>
                x.CreateAndStartContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.IsAny<string>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task ProcessWorkflowAsync_WhenRepoAllowedAndSelfHosted_StartsContainer()
    {
        var workflow = new Workflow(
            "queued",
            new WorkflowJob("job1", ["self-hosted"], 123),
            new Repository("myorg/repo", "repo")
        );

        _labelMatcherMock.Setup(x => x.HasAllRequiredLabels(It.IsAny<string[]>())).Returns(true);
        _repositoryFilterMock.Setup(x => x.IsRepositoryAllowed("myorg/repo")).Returns(true);
        _containerManagerMock
            .Setup(x =>
                x.CreateAndStartContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(true);

        var result = await _service.ProcessWorkflowAsync(workflow);

        result.Should().BeTrue();
        _containerManagerMock.Verify(
            x =>
                x.CreateAndStartContainerAsync(
                    "myorg/repo",
                    It.Is<string>(s => s.Contains("repo-123")),
                    123,
                    "test-image"
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task WaitForAvailableRunnerAsync_WaitsUntilSlotAvailable()
    {
        // Arrange
        _containerManagerMock
            .SetupSequence(x => x.ListContainersAsync())
            .ReturnsAsync(new List<ContainerListResponse> { new(), new() }) // 2 containers (full)
            .ReturnsAsync(new List<ContainerListResponse> { new() }); // 1 container (available)

        // Act
        await _service.WaitForAvailableRunnerAsync();

        // Assert
        _containerManagerMock.Verify(x => x.ListContainersAsync(), Times.Exactly(2));
    }
}
