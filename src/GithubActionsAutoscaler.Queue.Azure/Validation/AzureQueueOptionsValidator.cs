using Microsoft.Extensions.Options;

namespace GithubActionsAutoscaler.Queue.Azure.Validation;

public class AzureQueueOptionsValidator : IValidateOptions<AzureQueueOptions>
{
	public ValidateOptionsResult Validate(string? name, AzureQueueOptions options)
	{
		if (string.IsNullOrWhiteSpace(options.ConnectionString))
		{
			return ValidateOptionsResult.Fail("Azure Storage connection string required");
		}

	if (string.IsNullOrWhiteSpace(options.QueueName))
	{
		return ValidateOptionsResult.Fail("Azure Storage queue name required");
	}

	return ValidateOptionsResult.Success;
	}
}
