using AutoscalerApi;
using AutoscalerApi.Services;
using AutoscalerApi.Workers;
using Docker.DotNet;
using ApplicationJsonSerializerContext = AutoscalerApi.Models.ApplicationJsonSerializerContext;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.custom.json", true);
var appConfig = AppConfiguration.FromConfiguration(builder.Configuration);
/*builder.Configuration.Bind(appConfig);*/

// Add services to the container.
if (appConfig.UseWebEndpoint)
{
    builder.Services.AddControllers().AddJsonOptions(options =>
        options.JsonSerializerOptions.AddContext<ApplicationJsonSerializerContext>());

    builder.Services.AddEndpointsApiExplorer();
}

builder.Services.AddSingleton<IDockerService, DockerService>();
builder.Services.AddApplicationInsightsTelemetryWorkerService();

var dockerConfig = !string.IsNullOrWhiteSpace(appConfig.DockerHost)
    ? new DockerClientConfiguration(new Uri(appConfig.DockerHost))
    : new DockerClientConfiguration();
builder.Services.AddSingleton(_ => dockerConfig.CreateClient());

if (!string.IsNullOrWhiteSpace(appConfig.AzureStorage))
{
    builder.Services.AddHostedService<QueueMonitorWorker>();
}

builder.Services.AddSingleton(appConfig);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (appConfig.UseWebEndpoint)
{
    app.UseAuthorization();
    app.MapControllers();
}

app.Run();
