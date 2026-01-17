using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using GithubActionsAutoscaler.Configuration;
using GithubActionsAutoscaler.Models;

namespace GithubActionsAutoscaler.Services;

public class AzureQueueService : IQueueService
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<AzureQueueService> _logger;

    public AzureQueueService(AppConfiguration config, ILogger<AzureQueueService> logger)
    {
        // Using connection string and queue name as per Program.cs
        _queueClient = new QueueClient(config.AzureStorage, config.AzureStorageQueue);
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken token)
    {
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: token);
    }

    public async Task<Models.QueueMessage?> ReceiveMessageAsync(CancellationToken token)
    {
        try
        {
            var response = await _queueClient.ReceiveMessageAsync(cancellationToken: token);
            if (response.Value == null)
                return null;

            var azureMsg = response.Value;

            // Instruction: Decode the Base64 message body from Azure before returning it in QueueMessage.Body
            string body;
            try
            {
                var bytes = Convert.FromBase64String(azureMsg.MessageText);
                body = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                // Fallback if not base64? The original code assumed base64.
                body = azureMsg.MessageText;
                _logger.LogWarning(
                    "Message {MessageId} body was not valid Base64",
                    azureMsg.MessageId
                );
            }

            return new Models.QueueMessage(azureMsg.MessageId, azureMsg.PopReceipt, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving message from Azure Queue");
            throw;
        }
    }

    public async Task DeleteMessageAsync(
        string messageId,
        string popReceipt,
        CancellationToken token
    )
    {
        await _queueClient.DeleteMessageAsync(messageId, popReceipt, token);
    }

    public async Task<string?> PeekMessageIdAsync(CancellationToken token)
    {
        PeekedMessage? pms = await _queueClient.PeekMessageAsync(token);
        return pms?.MessageId;
    }
}
