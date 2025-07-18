using Autoscaler.Domain;
using Autoscaler.Worker;
using AutoscalerApi.Services;

var builder = Host.CreateApplicationBuilder(args);

var appConfig = AppConfiguration.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(appConfig);

builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();
builder.Services.AddDockerService(appConfig);

var host = builder.Build();
host.Run();
