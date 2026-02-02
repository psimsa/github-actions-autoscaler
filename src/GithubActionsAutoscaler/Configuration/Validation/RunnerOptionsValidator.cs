using Microsoft.Extensions.Options;

namespace GithubActionsAutoscaler.Configuration.Validation;

public class RunnerOptionsValidator : IValidateOptions<RunnerOptions>
{
	public ValidateOptionsResult Validate(string? name, RunnerOptions options)
	{
		if (options.Provider == "Docker")
		{
			if (options.MaxRunners <= 0)
				return ValidateOptionsResult.Fail("Runner MaxRunners must be greater than 0");
			if (string.IsNullOrWhiteSpace(options.Docker.Image))
				return ValidateOptionsResult.Fail("Runner image is required");
		}

		return ValidateOptionsResult.Success;
	}
}
