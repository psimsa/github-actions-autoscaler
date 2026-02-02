using GithubActionsAutoscaler.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.custom.json", true);

var options = builder.BindAutoscalerOptions();
builder.RegisterAutoscalerServices(options);

var app = builder.Build();
app.ConfigureAutoscalerPipeline(options.AppOptions);
app.Run();
