using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Configuration.Validation;

namespace GithubActionsAutoscaler.Tests.Unit.Configuration;

public class AppOptionsValidatorTests
{
	[Fact]
	public void Validate_WhenQueueMonitorWithoutToken_Fails()
	{
		var validator = new AppOptionsValidator();
		var options = new AppOptions
		{
			Mode = OperationMode.QueueMonitor,
			GithubToken = "",
			Labels = ["self-hosted"]
		};

		var result = validator.Validate(null, options);

		Assert.False(result.Succeeded);
	}

	[Fact]
	public void Validate_WhenLabelsMissingSelfHosted_Fails()
	{
		var validator = new AppOptionsValidator();
		var options = new AppOptions
		{
			Mode = OperationMode.Webhook,
			Labels = ["linux"]
		};

		var result = validator.Validate(null, options);

		Assert.False(result.Succeeded);
	}
}
