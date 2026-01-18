using Testcontainers.RabbitMq;

namespace GithubActionsAutoscaler.Tests.Integration.Fixtures;

public class RabbitMqFixture : IAsyncLifetime
{
	private readonly RabbitMqContainer _container;

	public RabbitMqFixture()
	{
		_container = new RabbitMqBuilder("rabbitmq:3-management")
			.WithUsername("guest")
			.WithPassword("guest")
			.Build();
	}

	public string Host => _container.Hostname;
	public int Port => _container.GetMappedPublicPort(5672);
	public string Username => "guest";
	public string Password => "guest";

	public async Task InitializeAsync()
	{
		await _container.StartAsync();
	}

	public async Task DisposeAsync()
	{
		await _container.StopAsync();
	}
}
