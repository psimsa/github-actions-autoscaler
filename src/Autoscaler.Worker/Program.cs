using Autoscaler.Worker;
using AutoscalerApi.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddDockerService();

var host = builder.Build();
host.Run();
