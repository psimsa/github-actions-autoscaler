var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Autoscaler_Api>("autoscaler-api");

builder.AddProject<Projects.Autoscaler_Worker>("autoscaler-worker");

builder.Build().Run();
