using System.Diagnostics;
using Azure.Storage.Queues;
using Docker.DotNet;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Endpoints;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Workers;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.custom.json", true);
var appConfig = AppConfiguration.FromConfiguration(builder.Configuration);

if (appConfig.UseWebEndpoint)
{
    builder.Services.AddEndpointsApiExplorer();
    // builder.Services.AddSwaggerGen();
}

builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton<IRepositoryFilter, RepositoryFilter>();
builder.Services.AddSingleton<ILabelMatcher, LabelMatcher>();
builder.Services.AddSingleton<IImageManager, ImageManager>();
builder.Services.AddSingleton<IContainerManager, ContainerManager>();
builder.Services.AddSingleton<IDockerService, DockerService>();

if (appConfig.OpenTelemetry.Enabled)
{
    var activitySource = new ActivitySource("GithubActionsAutoscaler");
    builder.Services.AddSingleton(activitySource);

    var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? appConfig.OpenTelemetry.OtlpEndpoint;
    builder
        .Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource.AddService(serviceName: Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? appConfig.OpenTelemetry.ServiceName)
        )
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource(activitySource.Name);

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
            }
        })
        .WithLogging(logging =>
        {
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                logging.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
            }
        })
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                metrics.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
            }
        });
}

var dockerConfig = !string.IsNullOrWhiteSpace(appConfig.DockerHost)
    ? new DockerClientConfiguration(new Uri(appConfig.DockerHost))
    : new DockerClientConfiguration();
builder.Services.AddSingleton(_ => dockerConfig.CreateClient());

if (!string.IsNullOrWhiteSpace(appConfig.AzureStorage))
{
    builder.Services.AddHostedService<QueueMonitorWorker>();
}

builder.Services.AddSingleton(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<AppConfiguration>();
    return new QueueClient(config.AzureStorage, config.AzureStorageQueue);
});

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
