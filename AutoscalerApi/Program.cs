using AutoscalerApi;
using AutoscalerApi.Services;
using AutoscalerApi.Workers;
using Docker.DotNet;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.custom.json", true);
var appConfig = AppConfiguration.FromConfiguration(builder.Configuration);
/*builder.Configuration.Bind(appConfig);*/

Console.WriteLine(appConfig.AzureStorageQueue);


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<IDockerService, DockerService>();
var dockerConfig = !string.IsNullOrWhiteSpace(appConfig.DockerHost) ? new DockerClientConfiguration(new Uri(appConfig.DockerHost)) : new DockerClientConfiguration();

builder.Services.AddSingleton(_ => dockerConfig.CreateClient());
if (!string.IsNullOrWhiteSpace(appConfig.AzureStorage))
{
    builder.Services.AddHostedService<QueueMonitorWorker>();
}

builder.Services.AddSingleton(appConfig);
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<AppConfiguration>>();
logger.LogInformation("{asdf}", appConfig.AzureStorageQueue);
foreach (var s in appConfig.RepoWhitelist)
{
    logger.LogInformation("{s}", s);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

if (appConfig.UseWebEndpoint)
{
    app.MapControllers();
}

app.Run();
