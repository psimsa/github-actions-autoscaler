using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Configuration.Validation;

namespace GithubActionsAutoscaler.Tests.Unit.Configuration;

public class RunnerOptionsValidatorTests
{
	[Fact]
	public void Validate_WhenDockerMissingImage_Fails()
	{
		var validator = new RunnerOptionsValidator();
		var options = new RunnerOptions
		{
			Provider = "Docker",
			MaxRunners = 2,
			Docker = new DockerRunnerOptions { Image = "" }
		};

		var result = validator.Validate(null, options);

		Assert.False(result.Succeeded);
	}
}
