using Testcontainers.Azurite;

namespace GithubActionsAutoscaler.Tests.Integration.Fixtures;

public class AzuriteFixture : IAsyncLifetime
{
	private readonly AzuriteContainer _container;

	public AzuriteFixture()
	{
		_container = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
			.WithCommand("--skipApiVersionCheck")
			.Build();
	}

	public string ConnectionString => _container.GetConnectionString();

	public async Task InitializeAsync()
	{
		await _container.StartAsync();
	}

	public async Task DisposeAsync()
	{
		await _container.StopAsync();
	}
}
