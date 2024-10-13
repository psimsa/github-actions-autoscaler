using Autoscaler.Domain;
using Azure.Storage.Queues;

namespace Autoscaler.Worker
{
    public class Worker : BackgroundService
    {
        private readonly QueueServiceClient _client;
        private readonly AppConfiguration _configuration;
        private readonly ILogger<Worker> _logger;

        public Worker(QueueServiceClient client, AppConfiguration configuration, ILogger<Worker> logger)
        {
            _client = client;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.GetQueueClient(_configuration.AzureStorageQueue).CreateIfNotExists();

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
