using GithubActionsAutoscaler.Models;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Workers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GithubActionsAutoscaler.Tests.Unit.Workers;

public class QueueMonitorWorkerTests
{
    private readonly Mock<IDockerService> _dockerServiceMock;
    private readonly Mock<IQueueService> _queueServiceMock;
    private readonly Mock<ILogger<QueueMonitorWorker>> _loggerMock;
    private readonly QueueMonitorWorker _worker;

    public QueueMonitorWorkerTests()
    {
        _dockerServiceMock = new Mock<IDockerService>();
        _queueServiceMock = new Mock<IQueueService>();
        _loggerMock = new Mock<ILogger<QueueMonitorWorker>>();

        _worker = new QueueMonitorWorker(
            _queueServiceMock.Object,
            _dockerServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ProcessNextMessageAsync_WhenNoMessage_WaitsAndContinues()
    {
        // Arrange
        _queueServiceMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueMessage?)null);

        // Act
        await _worker.ProcessNextMessageAsync(CancellationToken.None);

        // Assert
        _queueServiceMock.Verify(
            x => x.ReceiveMessageAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
        _dockerServiceMock.Verify(x => x.ProcessWorkflowAsync(It.IsAny<Workflow>()), Times.Never);
    }

    [Fact]
    public async Task ProcessNextMessageAsync_WhenMessageReceived_ProcessesWorkflow()
    {
        // Arrange
        var workflow = new Workflow(
            "queued",
            new WorkflowJob("job1", [], 1),
            new Repository("repo/name", "name")
        );
        var json = System.Text.Json.JsonSerializer.Serialize(workflow);

        // Service returns decoded body now
        var queueMessage = new QueueMessage("id", "receipt", json);

        _queueServiceMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(queueMessage);

        _dockerServiceMock
            .Setup(x => x.ProcessWorkflowAsync(It.IsAny<Workflow>()))
            .ReturnsAsync(true);

        // Act
        await _worker.ProcessNextMessageAsync(CancellationToken.None);

        // Assert
        _dockerServiceMock.Verify(
            x => x.ProcessWorkflowAsync(It.Is<Workflow>(w => w.Job.Name == "job1")),
            Times.Once
        );
        _queueServiceMock.Verify(
            x => x.DeleteMessageAsync("id", "receipt", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
