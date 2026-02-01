using Docker.DotNet;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Endpoints;
using GithubActionsAutoscaler.Extensions;
using GithubActionsAutoscaler.Queue.Azure;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.custom.json", true);
var appConfig = AppConfiguration.FromConfiguration(builder.Configuration);

if (appConfig.UseWebEndpoint)
{
    builder.Services.AddEndpointsApiExplorer();
    // builder.Services.AddSwaggerGen();
}

builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton<GithubActionsAutoscaler.Abstractions.Services.IRepositoryFilter>(_ =>
{
	return new GithubActionsAutoscaler.Abstractions.Services.RepositoryFilter(
		appConfig.RepoAllowlistPrefix,
		appConfig.RepoAllowlist,
		appConfig.IsRepoAllowlistExactMatch,
		appConfig.RepoDenylistPrefix,
		appConfig.RepoDenylist,
		appConfig.IsRepoDenylistExactMatch
	);
});
builder.Services.AddSingleton<GithubActionsAutoscaler.Abstractions.Services.ILabelMatcher>(
	_ => new GithubActionsAutoscaler.Abstractions.Services.LabelMatcher(appConfig.Labels)
);
builder.Services.AddSingleton<IImageManager, ImageManager>();
builder.Services.AddSingleton<IContainerManager, ContainerManager>();
builder.Services.AddSingleton<IDockerService, DockerService>();

if (appConfig.OpenTelemetry.Enabled)
{
    builder.Services.AddOpenTelemetryInstrumentation(appConfig);
}

var dockerConfig = !string.IsNullOrWhiteSpace(appConfig.DockerHost)
    ? new DockerClientConfiguration(new Uri(appConfig.DockerHost))
    : new DockerClientConfiguration();
builder.Services.AddSingleton(_ => dockerConfig.CreateClient());

if (!string.IsNullOrWhiteSpace(appConfig.AzureStorage))
{
	builder.Services.AddAzureQueueProvider(
		new AzureQueueOptions
		{
			ConnectionString = appConfig.AzureStorage,
			QueueName = appConfig.AzureStorageQueue
		}
	);
	builder.Services.AddHostedService<QueueMonitorWorker>();
}

var app = builder.Build();

if (appConfig.UseWebEndpoint)
{
    app.UseHttpsRedirection();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapGet("/", () => Results.Ok("alive"));

    app.MapWorkflowEndpoints();
}

app.Run();
