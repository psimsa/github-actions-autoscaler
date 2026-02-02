using Microsoft.Extensions.Options;

namespace GithubActionsAutoscaler.Configuration.Validation;

public class QueueOptionsValidator : IValidateOptions<QueueOptions>
{
	public ValidateOptionsResult Validate(string? name, QueueOptions options)
	{
		if (options.Provider == "AzureStorageQueue")
		{
			if (string.IsNullOrWhiteSpace(options.AzureStorageQueue.ConnectionString))
				return ValidateOptionsResult.Fail("Azure Storage connection string required");
			if (string.IsNullOrWhiteSpace(options.AzureStorageQueue.QueueName))
				return ValidateOptionsResult.Fail("Azure Storage queue name required");
		}

		return ValidateOptionsResult.Success;
	}
}
