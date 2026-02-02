using Docker.DotNet;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Configuration.Validation;
using GithubActionsAutoscaler.Endpoints;
using GithubActionsAutoscaler.Extensions;
using GithubActionsAutoscaler.Queue.Azure;
using GithubActionsAutoscaler.Runner.Docker;
using GithubActionsAutoscaler.Runner.Docker.Services;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Workers;
using GithubActionsAutoscaler.Abstractions.Telemetry;
using Microsoft.Extensions.Options;
using QueueAzureOptions = GithubActionsAutoscaler.Queue.Azure.AzureQueueOptions;
using RunnerDockerOptions = GithubActionsAutoscaler.Runner.Docker.DockerRunnerOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.custom.json", true);

var appOptions = BindOptions();
var queueOptions = builder.Configuration.GetSection("Queue").Get<QueueOptions>() ?? new QueueOptions();
var runnerOptions = builder.Configuration.GetSection("Runner").Get<RunnerOptions>() ?? new RunnerOptions();

RegisterEndpoints(appOptions);
RegisterCoreServices(appOptions, queueOptions, runnerOptions);
RegisterObservability(appOptions);
RegisterQueueProvider(queueOptions);
RegisterRunnerServices(appOptions, runnerOptions);

var app = builder.Build();

if (appOptions.Mode is OperationMode.Webhook or OperationMode.Both)
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

AppOptions BindOptions()
{
	builder.Services.AddOptions<AppOptions>()
		.Bind(builder.Configuration.GetSection("App"))
		.ValidateOnStart();
	builder.Services.AddOptions<QueueOptions>()
		.Bind(builder.Configuration.GetSection("Queue"))
		.ValidateOnStart();
	builder.Services.AddOptions<RunnerOptions>()
		.Bind(builder.Configuration.GetSection("Runner"))
		.ValidateOnStart();

	builder.Services.AddSingleton<IValidateOptions<AppOptions>, AppOptionsValidator>();
	builder.Services.AddSingleton<IValidateOptions<QueueOptions>, QueueOptionsValidator>();
	builder.Services.AddSingleton<IValidateOptions<RunnerOptions>, RunnerOptionsValidator>();

	return builder.Configuration.GetSection("App").Get<AppOptions>() ?? new AppOptions();
}

void RegisterEndpoints(AppOptions options)
{
	if (options.Mode is OperationMode.Webhook or OperationMode.Both)
	{
		builder.Services.AddEndpointsApiExplorer();
		// builder.Services.AddSwaggerGen();
	}
}

void RegisterCoreServices(AppOptions appOptions, QueueOptions queueOptions, RunnerOptions runnerOptions)
{
	builder.Services.AddSingleton(appOptions);
	builder.Services.AddSingleton(queueOptions);
	builder.Services.AddSingleton(runnerOptions);
	builder.Services.AddSingleton<GithubActionsAutoscaler.Abstractions.Services.IRepositoryFilter>(_ =>
	{
		var filter = appOptions.RepositoryFilter;
		return new GithubActionsAutoscaler.Abstractions.Services.RepositoryFilter(
			filter.AllowlistPrefix,
			filter.Allowlist,
			filter.IsAllowlistExactMatch,
			filter.DenylistPrefix,
			filter.Denylist,
			filter.IsDenylistExactMatch
		);
	});
	builder.Services.AddSingleton<GithubActionsAutoscaler.Abstractions.Services.ILabelMatcher>(
		_ => new GithubActionsAutoscaler.Abstractions.Services.LabelMatcher(appOptions.Labels)
	);
	builder.Services.AddSingleton<IWorkflowProcessor, WorkflowProcessor>();

	var dockerConfig = !string.IsNullOrWhiteSpace(runnerOptions.Docker.Host)
		? new DockerClientConfiguration(new Uri(runnerOptions.Docker.Host))
		: new DockerClientConfiguration();
	builder.Services.AddSingleton(_ => dockerConfig.CreateClient());
}

void RegisterObservability(AppOptions options)
{
	if (options.OpenTelemetry.Enabled)
	{
		builder.Services.AddOpenTelemetryInstrumentation(options.OpenTelemetry);
	}
}

void RegisterQueueProvider(QueueOptions queueOptions)
{
	builder.Services.AddAzureQueueProvider(
		new QueueAzureOptions
		{
			ConnectionString = queueOptions.AzureStorageQueue.ConnectionString,
			QueueName = queueOptions.AzureStorageQueue.QueueName
		}
	);
}

void RegisterRunnerServices(AppOptions appOptions, RunnerOptions runnerOptions)
{
	if (appOptions.Mode is OperationMode.QueueMonitor or OperationMode.Both)
	{
		builder.Services.AddHostedService<QueueMonitorWorker>();
		builder.Services.AddDockerRunnerProvider(
			new RunnerDockerOptions
			{
				MaxRunners = runnerOptions.MaxRunners,
				Image = runnerOptions.Docker.Image,
				DockerHost = runnerOptions.Docker.Host,
				RegistryToken = runnerOptions.Docker.RegistryToken,
				AccessToken = appOptions.GithubToken,
				AutoCheckForImageUpdates = runnerOptions.Docker.AutoCheckForImageUpdates,
				ToolCacheVolumeName = runnerOptions.Docker.ToolCacheVolumeName,
				CoordinatorHostname = appOptions.CoordinatorHostname,
				Labels = appOptions.Labels
			}
		);
		if (appOptions.OpenTelemetry.Enabled)
		{
			builder.Services.AddSingleton<AutoscalerMetrics>();
			// TODO: Add background stats updater for metrics.
		}
	}
}
