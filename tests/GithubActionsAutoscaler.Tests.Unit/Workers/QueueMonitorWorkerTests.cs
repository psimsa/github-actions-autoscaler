using System.Diagnostics;
using GithubActionsAutoscaler.Abstractions.Models;
using GithubActionsAutoscaler.Abstractions.Queue;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Workers;
using Microsoft.Extensions.Logging;

namespace GithubActionsAutoscaler.Tests.Unit.Workers;

public class QueueMonitorWorkerTests
{
    private readonly Mock<IDockerService> _dockerServiceMock;
	private readonly Mock<IQueueProvider> _queueProviderMock;
	private readonly Mock<ILogger<QueueMonitorWorker>> _loggerMock;
	private readonly QueueMonitorWorker _worker;

    public QueueMonitorWorkerTests()
    {
        _dockerServiceMock = new Mock<IDockerService>();
		_queueProviderMock = new Mock<IQueueProvider>();
		_loggerMock = new Mock<ILogger<QueueMonitorWorker>>();

        // We need to use the constructor that we are about to create/modify
        // For now, I'll assume the new signature
		_worker = new QueueMonitorWorker(
			_queueProviderMock.Object,
			_dockerServiceMock.Object,
			new ActivitySource("GithubActionsAutoscaler.Tests"),
			_loggerMock.Object
		);
	}

    [Fact]
    public async Task ProcessNextMessageAsync_WhenNoMessage_WaitsAndContinues()
    {
        // Arrange
		_queueProviderMock
			.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		_queueProviderMock
			.Setup(x => x.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((IQueueMessage?)null);

        // Act
        // We need to expose this method or make it internal and use InternalsVisibleTo
        // For now, I'll assume I can test the logic via a public method or internal
        await _worker.ProcessNextMessageAsync(CancellationToken.None);

        // Assert
		_queueProviderMock.Verify(
			x => x.ReceiveMessageAsync(It.IsAny<CancellationToken>()),
			Times.Once
		);
        _dockerServiceMock.Verify(x => x.ProcessWorkflowAsync(It.IsAny<Workflow>()), Times.Never);
    }

    [Fact]
    public async Task ProcessNextMessageAsync_WhenMessageReceived_ProcessesWorkflow()
    {
        // Arrange
		// "body" needs to be base64 encoded json
		var workflow = new Workflow(
			"queued",
			new WorkflowJob("job1", [], 1),
			new Repository("repo/name", "name")
		);
		var json = System.Text.Json.JsonSerializer.Serialize(workflow);
		var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

		var queueMessage = new TestQueueMessage("id", "receipt", base64, 0);

		_queueProviderMock
			.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		_queueProviderMock
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
		_queueProviderMock.Verify(
			x => x.DeleteMessageAsync(queueMessage, It.IsAny<CancellationToken>()),
			Times.Once
		);
	}

	private sealed record TestQueueMessage(
		string MessageId,
		string PopReceipt,
		string Content,
		int DequeueCount
	) : IQueueMessage
	{
		public DateTimeOffset? InsertedOn { get; } = DateTimeOffset.UtcNow;
	}
}
