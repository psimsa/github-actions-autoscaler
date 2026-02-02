using Docker.DotNet;
using GithubActionsAutoscaler.Abstractions.Telemetry;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Configuration.Validation;
using GithubActionsAutoscaler.Endpoints;
using GithubActionsAutoscaler.Queue.Azure;
using GithubActionsAutoscaler.Runner.Docker;
using GithubActionsAutoscaler.Runner.Docker.Services;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Workers;
using Microsoft.Extensions.Options;
using QueueAzureOptions = GithubActionsAutoscaler.Queue.Azure.AzureQueueOptions;
using RunnerDockerOptions = GithubActionsAutoscaler.Runner.Docker.DockerRunnerOptions;

namespace GithubActionsAutoscaler.Extensions;

public static class AutoscalerSetupExtensions
{
	public static AutoscalerOptions BindAutoscalerOptions(this WebApplicationBuilder builder)
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

		var appOptions = builder.Configuration.GetSection("App").Get<AppOptions>() ?? new AppOptions();
		var queueOptions = builder.Configuration.GetSection("Queue").Get<QueueOptions>() ?? new QueueOptions();
		var runnerOptions = builder.Configuration.GetSection("Runner").Get<RunnerOptions>() ?? new RunnerOptions();

		return new AutoscalerOptions(appOptions, queueOptions, runnerOptions);
	}

	public static WebApplicationBuilder RegisterAutoscalerServices(
		this WebApplicationBuilder builder,
		AutoscalerOptions options)
	{
		RegisterEndpoints(builder, options.AppOptions);
		RegisterCoreServices(builder, options);
		RegisterObservability(builder, options.AppOptions);
		RegisterQueueProvider(builder, options.QueueOptions);
		RegisterRunnerServices(builder, options);
		return builder;
	}

	public static WebApplication ConfigureAutoscalerPipeline(
		this WebApplication app,
		AppOptions appOptions)
	{
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

		return app;
	}

	private static void RegisterEndpoints(WebApplicationBuilder builder, AppOptions options)
	{
		if (options.Mode is OperationMode.Webhook or OperationMode.Both)
		{
			builder.Services.AddEndpointsApiExplorer();
			// builder.Services.AddSwaggerGen();
		}
	}

	private static void RegisterCoreServices(WebApplicationBuilder builder, AutoscalerOptions options)
	{
		builder.Services.AddSingleton(options.AppOptions);
		builder.Services.AddSingleton(options.QueueOptions);
		builder.Services.AddSingleton(options.RunnerOptions);
		builder.Services.AddSingleton<GithubActionsAutoscaler.Abstractions.Services.IRepositoryFilter>(_ =>
		{
			var filter = options.AppOptions.RepositoryFilter;
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
			_ => new GithubActionsAutoscaler.Abstractions.Services.LabelMatcher(options.AppOptions.Labels)
		);
		builder.Services.AddSingleton<IWorkflowProcessor, WorkflowProcessor>();

		var dockerConfig = !string.IsNullOrWhiteSpace(options.RunnerOptions.Docker.Host)
			? new DockerClientConfiguration(new Uri(options.RunnerOptions.Docker.Host))
			: new DockerClientConfiguration();
		builder.Services.AddSingleton(_ => dockerConfig.CreateClient());
	}

	private static void RegisterObservability(WebApplicationBuilder builder, AppOptions options)
	{
		if (options.OpenTelemetry.Enabled)
		{
			builder.Services.AddOpenTelemetryInstrumentation(options.OpenTelemetry);
		}
	}

	private static void RegisterQueueProvider(WebApplicationBuilder builder, QueueOptions queueOptions)
	{
		builder.Services.AddAzureQueueProvider(
			new QueueAzureOptions
			{
				ConnectionString = queueOptions.AzureStorageQueue.ConnectionString,
				QueueName = queueOptions.AzureStorageQueue.QueueName
			}
		);
	}

	private static void RegisterRunnerServices(WebApplicationBuilder builder, AutoscalerOptions options)
	{
		if (options.AppOptions.Mode is OperationMode.QueueMonitor or OperationMode.Both)
		{
			builder.Services.AddHostedService<QueueMonitorWorker>();
			builder.Services.AddDockerRunnerProvider(
				new RunnerDockerOptions
				{
					MaxRunners = options.RunnerOptions.MaxRunners,
					Image = options.RunnerOptions.Docker.Image,
					DockerHost = options.RunnerOptions.Docker.Host,
					RegistryToken = options.RunnerOptions.Docker.RegistryToken,
					AccessToken = options.AppOptions.GithubToken,
					AutoCheckForImageUpdates = options.RunnerOptions.Docker.AutoCheckForImageUpdates,
					ToolCacheVolumeName = options.RunnerOptions.Docker.ToolCacheVolumeName,
					CoordinatorHostname = options.AppOptions.CoordinatorHostname,
					Labels = options.AppOptions.Labels
				}
			);
			if (options.AppOptions.OpenTelemetry.Enabled)
			{
				builder.Services.AddSingleton<AutoscalerMetrics>();
				// TODO: Add background stats updater for metrics.
			}
		}
	}
}
