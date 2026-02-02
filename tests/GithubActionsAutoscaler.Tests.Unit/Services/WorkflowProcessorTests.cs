using GithubActionsAutoscaler.Abstractions.Models;
using GithubActionsAutoscaler.Abstractions.Runner;
using GithubActionsAutoscaler.Abstractions.Services;
using GithubActionsAutoscaler.Services;
using Microsoft.Extensions.Logging;

namespace GithubActionsAutoscaler.Tests.Unit.Services;

public class WorkflowProcessorTests
{
	private readonly Mock<IRunnerManager> _runnerManagerMock;
	private readonly Mock<IRepositoryFilter> _repositoryFilterMock;
	private readonly Mock<ILabelMatcher> _labelMatcherMock;
	private readonly Mock<ILogger<WorkflowProcessor>> _loggerMock;
	private readonly WorkflowProcessor _processor;

	public WorkflowProcessorTests()
	{
		_runnerManagerMock = new Mock<IRunnerManager>();
		_repositoryFilterMock = new Mock<IRepositoryFilter>();
		_labelMatcherMock = new Mock<ILabelMatcher>();
		_loggerMock = new Mock<ILogger<WorkflowProcessor>>();
		_processor = new WorkflowProcessor(
			_runnerManagerMock.Object,
			_repositoryFilterMock.Object,
			_labelMatcherMock.Object,
			_loggerMock.Object
		);
	}

	[Fact]
	public async Task ProcessWorkflowAsync_WhenLabelsDoNotMatch_ReturnsFalse()
	{
		var workflow = new Workflow(
			"queued",
			new WorkflowJob("job1", ["self-hosted", "wrong"], 1),
			new Repository("repo", "name")
		);
		_labelMatcherMock.Setup(x => x.HasAllRequiredLabels(It.IsAny<string[]>())).Returns(false);

		var result = await _processor.ProcessWorkflowAsync(workflow);

		Assert.False(result);
	}

	[Fact]
	public async Task ProcessWorkflowAsync_WhenRepoAllowed_StartsRunner()
	{
		var workflow = new Workflow(
			"queued",
			new WorkflowJob("job1", ["self-hosted"], 123),
			new Repository("myorg/repo", "repo")
		);

		_labelMatcherMock.Setup(x => x.HasAllRequiredLabels(It.IsAny<string[]>())).Returns(true);
		_repositoryFilterMock.Setup(x => x.IsRepositoryAllowed("myorg/repo")).Returns(true);
		_runnerManagerMock
			.Setup(x => x.CanCreateRunnerAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		_runnerManagerMock
			.Setup(x => x.CreateRunnerAsync("myorg/repo", It.IsAny<string>(), 123, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Mock<IRunnerInstance>().Object);

		var result = await _processor.ProcessWorkflowAsync(workflow);

		Assert.True(result);
		_runnerManagerMock.Verify(
			x => x.CanCreateRunnerAsync(It.IsAny<CancellationToken>()),
			Times.Once
		);
	}
}
