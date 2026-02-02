using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Configuration.Validation;

namespace GithubActionsAutoscaler.Tests.Unit.Configuration;

public class QueueOptionsValidatorTests
{
	[Fact]
	public void Validate_WhenAzureMissingConnectionString_Fails()
	{
		var validator = new QueueOptionsValidator();
		var options = new QueueOptions
		{
			Provider = "AzureStorageQueue",
			AzureStorageQueue = new AzureQueueOptions
			{
				ConnectionString = "",
				QueueName = "queue"
			}
		};

		var result = validator.Validate(null, options);

		Assert.False(result.Succeeded);
	}
}
