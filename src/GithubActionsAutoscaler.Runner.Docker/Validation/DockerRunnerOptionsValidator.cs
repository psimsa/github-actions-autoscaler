using Microsoft.Extensions.Options;

namespace GithubActionsAutoscaler.Runner.Docker.Validation;

public class DockerRunnerOptionsValidator : IValidateOptions<DockerRunnerOptions>
{
	public ValidateOptionsResult Validate(string? name, DockerRunnerOptions options)
	{
		if (options.MaxRunners <= 0)
		{
			return ValidateOptionsResult.Fail("Runner MaxRunners must be greater than 0");
		}

		if (string.IsNullOrWhiteSpace(options.Image))
		{
			return ValidateOptionsResult.Fail("Runner image is required");
		}

		return ValidateOptionsResult.Success;
	}
}
