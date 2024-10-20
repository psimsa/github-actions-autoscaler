using AutoscalerApi;
using AutoscalerApi.Services;
using AutoscalerApi.Workers;
using Azure.Storage.Queues;
using Docker.DotNet;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.custom.json", true);
var appConfig = AppConfiguration.FromConfiguration(builder.Configuration);

if (appConfig.UseWebEndpoint)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

builder.Services.AddSingleton<IDockerService, DockerService>();
if (!string.IsNullOrWhiteSpace(appConfig.ApplicationInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetryWorkerService();
}

var dockerConfig = !string.IsNullOrWhiteSpace(appConfig.DockerHost)
    ? new DockerClientConfiguration(new Uri(appConfig.DockerHost))
    : new DockerClientConfiguration();
builder.Services.AddSingleton(_ => dockerConfig.CreateClient());

if (!string.IsNullOrWhiteSpace(appConfig.AzureStorage))
{
    builder.Services.AddHostedService<QueueMonitorWorker>();
}

builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(serviceProvider =>
{
    var appConfig = serviceProvider.GetRequiredService<AppConfiguration>();
    return new QueueClient(appConfig.AzureStorage, appConfig.AzureStorageQueue);
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
