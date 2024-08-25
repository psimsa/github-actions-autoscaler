using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var queueName = builder.AddParameter("AzureStorageQueue", secret: false);

IResourceBuilder<IResourceWithConnectionString> azureStorage;
if (builder.Environment.IsDevelopment())
{
    var storage = builder.AddAzureStorage("storage");
    storage.RunAsEmulator(cc =>
    {
        // cc.WithArgs("azurite", "--skipApiVersionCheck", "--loose");
    });
    azureStorage = storage.AddQueues("AzureStorage");
}
else
{
    azureStorage = builder.AddConnectionString("AzureStorage");
}

builder.AddProject<Projects.Autoscaler_Api>("autoscaler-api")
    .WithReference(azureStorage)
    .WithEnvironment("AzureStorageQueue", queueName)
    ;

builder.AddProject<Projects.Autoscaler_Worker>("autoscaler-worker")
    .WithReference(azureStorage)
    .WithEnvironment("AzureStorageQueue", queueName)
    ;

builder.Build().Run();
