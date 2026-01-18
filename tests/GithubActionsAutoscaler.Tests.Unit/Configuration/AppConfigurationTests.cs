using FluentAssertions;
using GithubActionsAutoscaler.Configuration;
using Microsoft.Extensions.Configuration;

namespace GithubActionsAutoscaler.Tests.Unit.Configuration;

public class AppConfigurationTests
{
	[Fact]
	public void FromConfiguration_WhenQueueProviderExplicitlySetToRabbitMQ_ReturnsRabbitMQ()
	{
		var config = CreateConfiguration(new Dictionary<string, string?>
		{
			{ "QueueProvider", "RabbitMQ" }
		});

		var appConfig = AppConfiguration.FromConfiguration(config);

		appConfig.QueueProvider.Should().Be(QueueProvider.RabbitMQ);
	}

	[Fact]
	public void FromConfiguration_WhenQueueProviderExplicitlySetToAzure_ReturnsAzure()
	{
		var config = CreateConfiguration(new Dictionary<string, string?>
		{
			{ "QueueProvider", "Azure" }
		});

		var appConfig = AppConfiguration.FromConfiguration(config);

		appConfig.QueueProvider.Should().Be(QueueProvider.Azure);
	}

	[Fact]
	public void FromConfiguration_WhenQueueProviderIsCaseInsensitive_ParsesCorrectly()
	{
		var config = CreateConfiguration(new Dictionary<string, string?>
		{
			{ "QueueProvider", "rabbitmq" }
		});

		var appConfig = AppConfiguration.FromConfiguration(config);

		appConfig.QueueProvider.Should().Be(QueueProvider.RabbitMQ);
	}

	[Fact]
	public void FromConfiguration_WhenAzureStorageProvidedWithoutExplicitProvider_DefaultsToAzure()
	{
		var config = CreateConfiguration(new Dictionary<string, string?>
		{
			{ "AzureStorage", "DefaultEndpointsProtocol=https;AccountName=test" }
		});

		var appConfig = AppConfiguration.FromConfiguration(config);

		appConfig.QueueProvider.Should().Be(QueueProvider.Azure);
	}

	[Fact]
	public void FromConfiguration_WhenNoQueueConfigProvided_DefaultsToAzure()
	{
		var config = CreateConfiguration(new Dictionary<string, string?>());

		var appConfig = AppConfiguration.FromConfiguration(config);

		appConfig.QueueProvider.Should().Be(QueueProvider.Azure);
	}

	[Fact]
	public void FromConfiguration_WhenExplicitProviderAndAzureStorageBothProvided_UsesExplicitProvider()
	{
		var config = CreateConfiguration(new Dictionary<string, string?>
		{
			{ "QueueProvider", "RabbitMQ" },
			{ "AzureStorage", "DefaultEndpointsProtocol=https;AccountName=test" }
		});

		var appConfig = AppConfiguration.FromConfiguration(config);

		appConfig.QueueProvider.Should().Be(QueueProvider.RabbitMQ);
	}

	[Fact]
	public void FromConfiguration_WhenRabbitMQConfigProvided_SetsRabbitMQProperties()
	{
		var config = CreateConfiguration(new Dictionary<string, string?>
		{
			{ "QueueProvider", "RabbitMQ" },
			{ "RabbitHost", "localhost" },
			{ "RabbitPort", "5672" },
			{ "RabbitUsername", "guest" },
			{ "RabbitPassword", "guest" },
			{ "RabbitQueueName", "test-queue" }
		});

		var appConfig = AppConfiguration.FromConfiguration(config);

		appConfig.RabbitHost.Should().Be("localhost");
		appConfig.RabbitPort.Should().Be(5672);
		appConfig.RabbitUsername.Should().Be("guest");
		appConfig.RabbitPassword.Should().Be("guest");
		appConfig.RabbitQueueName.Should().Be("test-queue");
	}

	[Fact]
	public void FromConfiguration_WhenAzureConfigProvided_SetsAzureProperties()
	{
		var config = CreateConfiguration(new Dictionary<string, string?>
		{
			{ "AzureStorage", "DefaultEndpointsProtocol=https;AccountName=test" },
			{ "AzureStorageQueue", "github-actions" }
		});

		var appConfig = AppConfiguration.FromConfiguration(config);

		appConfig.AzureStorage.Should().Be("DefaultEndpointsProtocol=https;AccountName=test");
		appConfig.AzureStorageQueue.Should().Be("github-actions");
	}

	private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
	{
		return new ConfigurationBuilder()
			.AddInMemoryCollection(values)
			.Build();
	}
}
