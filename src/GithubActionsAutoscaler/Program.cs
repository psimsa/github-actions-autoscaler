using Azure.Storage.Queues;
using Docker.DotNet;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Endpoints;
using GithubActionsAutoscaler.Services;
using GithubActionsAutoscaler.Workers;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.custom.json", true);
var appConfig = AppConfiguration.FromConfiguration(builder.Configuration);

if (appConfig.UseWebEndpoint)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton<IRepositoryFilter, RepositoryFilter>();
builder.Services.AddSingleton<ILabelMatcher, LabelMatcher>();
builder.Services.AddSingleton<IImageManager, ImageManager>();
builder.Services.AddSingleton<IContainerManager, ContainerManager>();
builder.Services.AddSingleton<IDockerService, DockerService>();

if (appConfig.OpenTelemetry.Enabled)
{
    builder
        .Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource.AddService(serviceName: appConfig.OpenTelemetry.ServiceName)
        )
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();

            // Only add OTLP exporter if endpoint is configured (via config or env var)
            var otlpEndpoint =
                appConfig.OpenTelemetry.OtlpEndpoint
                ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter();
            }
        })
        .WithLogging(logging =>
        {
            var otlpEndpoint =
                appConfig.OpenTelemetry.OtlpEndpoint
                ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                logging.AddOtlpExporter();
            }
        });
}

var dockerConfig = !string.IsNullOrWhiteSpace(appConfig.DockerHost)
    ? new DockerClientConfiguration(new Uri(appConfig.DockerHost))
    : new DockerClientConfiguration();
builder.Services.AddSingleton(_ => dockerConfig.CreateClient());

if (appConfig.QueueProvider == QueueProvider.RabbitMQ)
{
    builder.Services.AddSingleton<IQueueService, RabbitMQQueueService>();
    builder.Services.AddHostedService<QueueMonitorWorker>();
}
else if (!string.IsNullOrWhiteSpace(appConfig.AzureStorage))
{
    builder.Services.AddSingleton<IQueueService, AzureQueueService>();
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
