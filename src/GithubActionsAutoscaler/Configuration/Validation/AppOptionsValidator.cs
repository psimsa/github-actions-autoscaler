using Microsoft.Extensions.Options;

namespace GithubActionsAutoscaler.Configuration.Validation;

public class AppOptionsValidator : IValidateOptions<AppOptions>
{
	public ValidateOptionsResult Validate(string? name, AppOptions options)
	{
		var failures = new List<string>();

		if (options.Mode is OperationMode.QueueMonitor or OperationMode.Both)
		{
			if (string.IsNullOrWhiteSpace(options.GithubToken))
				failures.Add("GithubToken is required for QueueMonitor mode");
		}

		if (!options.Labels.Contains("self-hosted", StringComparer.OrdinalIgnoreCase))
			failures.Add("Labels must include 'self-hosted'");

		return failures.Count > 0
			? ValidateOptionsResult.Fail(failures)
			: ValidateOptionsResult.Success;
	}
}
