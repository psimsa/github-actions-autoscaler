using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var queueName = builder.AddParameter("AzureStorageQueue", secret: false);

IResourceBuilder<IResourceWithConnectionString> azureStorage;

azureStorage = builder.ExecutionContext.IsPublishMode
    ? builder.AddConnectionString("AzureStorage")
    : builder.AddAzureStorage("storage")
        .RunAsEmulator().AddQueues("AzureStorage");


var dockerToken = builder.AddParameter("DockerToken", secret: true);
var githubToken = builder.AddParameter("GithubToken", secret: true);

builder.AddProject<Projects.Autoscaler_Api>("autoscaler-api")
    .WithReference(azureStorage)
    .WithEnvironment("AzureStorageQueue", queueName)
    ;

builder.AddProject<Projects.Autoscaler_Worker>("autoscaler-worker")
    .WithReference(azureStorage)
    .WithEnvironment("AzureStorageQueue", queueName)
    .WithEnvironment("DockerToken", dockerToken)
    .WithEnvironment("GithubToken", githubToken)
    ;

builder.Build().Run();
